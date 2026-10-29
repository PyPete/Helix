using Helix.Orchestration.Agent;
using Helix.Orchestration.Agent.Events;
using Helix.Orchestration.Agent.Execution;
using Helix.Orchestration.Agent.Leasing;
using Helix.Orchestration.Agent.Streaming;
using Helix.Orchestration.Agent.Trends;
using Helix.Orchestration.Agent.Security;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Leasing;
using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.Safety;
using Helix.Orchestration.Core.Trends;
using Helix.Orchestration.Agent.Api;
using Helix.Orchestration.Agent.Experiments;
using Helix.Orchestration.Infrastructure.Persistence;
using Helix.Orchestration.Agent.GrpcServer;
using Helix.Orchestration.Core.Devices;
using Helix.Orchestration.Core.WSL;
using Helix.Orchestration.Devices.Transports.Null;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .WriteTo.File(new RenderedCompactJsonFormatter(), "logs/helix-agent-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Log.Information("Helix Agent starting up.");

    var builder = WebApplication.CreateBuilder(args);
    ValidateLocalOnlyListeners(builder.Configuration);

    // Serialize enums as strings so TypeScript can do param.type === "Numeric" etc.
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

    // Serilog
    builder.Host.UseSerilog((_, services, config) => config
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(new RenderedCompactJsonFormatter())
        .WriteTo.File(new RenderedCompactJsonFormatter(), "logs/helix-agent-.log", rollingInterval: RollingInterval.Day));

    // OpenTelemetry
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing => tracing
            .AddSource(Helix.Orchestration.Agent.Telemetry.HelixTelemetry.TelemetryName))
        .WithMetrics(metrics => metrics
            .AddMeter(Helix.Orchestration.Agent.Telemetry.HelixTelemetry.TelemetryName));

    // Core leasing
    builder.Services.AddSingleton<InMemoryDeviceLeasingService>();
    builder.Services.AddSingleton<IDeviceLeasingService>(sp =>
        sp.GetRequiredService<InMemoryDeviceLeasingService>());

    // The safety boundary is proven against the simulator first. Do not register
    // hardware adapter bridges here: a registry entry is otherwise an alternate
    // mutable authority outside ICommandDispatcher.
    builder.Services.AddSingleton<NullDeviceTransport>();
    builder.Services.AddSingleton<Helix.Orchestration.Devices.Drivers.SimulatorDevice>();

    builder.Services.AddSingleton<SafetyEngine>();
    builder.Services.AddSingleton<ISafetyEngine>(sp => sp.GetRequiredService<SafetyEngine>());
    builder.Services.AddSingleton<InMemoryTrendProvider>();
    builder.Services.AddSingleton<ITrendProvider>(sp => sp.GetRequiredService<InMemoryTrendProvider>());
    builder.Services.AddSingleton<TrendAggregator>();
    builder.Services.AddSingleton<IRunspaceManager>(sp =>
    {
        var emitter = sp.GetService<IRunspaceEventEmitter>();
        var experimentManager = sp.GetService<IExperimentManager>();
        var dispatcher = sp.GetRequiredService<ICommandDispatcher>();
        var executionLogger = sp.GetRequiredService<ILogger<RunspaceManager>>();

        return new RunspaceManager(async (definition, ct) =>
        {
            // The runspace only coordinates canonical command steps. It never
            // receives CapabilityRegistry, a device adapter, or a leasing handle.
            Func<CancellationToken, Task>? body = null;
            var runspace = new InMemoryRunspace(definition, token => body!(token));
            var runspaceId = runspace.RunspaceId;

            body = async token =>
            {
                var experiment = experimentManager?.ListActive()
                    .FirstOrDefault(item => item.ActiveRunspaceId == runspaceId);
                if (experiment is null || !experimentManager!.TryGetCanonicalDefinition(experiment.ExperimentId, out var canonical) || canonical is null)
                {
                    throw new InvalidOperationException("A runspace may execute only an Agent-held canonical experiment definition.");
                }

                var executionId = Guid.ParseExact(runspaceId, "N");
                var executionEdges = definition.Edges.Where(edge =>
                    edge.EdgeKind is null || string.Equals(edge.EdgeKind, "exec", StringComparison.OrdinalIgnoreCase)).ToList();
                var inDegree = definition.Nodes.ToDictionary(node => node.NodeId, _ => 0);
                foreach (var edge in executionEdges)
                {
                    if (inDegree.ContainsKey(edge.TargetNodeId))
                        inDegree[edge.TargetNodeId]++;
                }

                var queue = new Queue<WslNode>(definition.Nodes.Where(node => inDegree[node.NodeId] == 0));
                var visited = new HashSet<string>(StringComparer.Ordinal);
                while (queue.Count > 0)
                {
                    token.ThrowIfCancellationRequested();
                    await runspace.CheckPauseAsync(token).ConfigureAwait(false);
                    var node = queue.Dequeue();
                    if (!visited.Add(node.NodeId))
                        continue;

                    var isStep = node.NodeType.Equals("Step", StringComparison.OrdinalIgnoreCase) ||
                                 node.NodeType.Equals("DeviceAction", StringComparison.OrdinalIgnoreCase) ||
                                 node.NodeType.Equals("Delay", StringComparison.OrdinalIgnoreCase);
                    if (isStep)
                    {
                        emitter?.Publish(new StepStartedEvent(runspaceId, node.NodeId, node.Label)
                        {
                            ExperimentId = experiment.ExperimentId,
                            ExperimentPhase = experiment.ActivePhase?.ToString().ToLowerInvariant()
                        });
                    }

                    try
                    {
                        if (node.NodeType.Equals("Step", StringComparison.OrdinalIgnoreCase) ||
                            node.NodeType.Equals("DeviceAction", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!node.Parameters.TryGetValue("capability", out var capabilityValue) || capabilityValue is not string registeredCapability ||
                                !registeredCapability.StartsWith("simulator.", StringComparison.Ordinal))
                            {
                                throw new InvalidOperationException("Workflow steps must use a canonical simulator capability.");
                            }

                            var capability = registeredCapability["simulator.".Length..];
                            var parameters = node.Parameters
                                .Where(pair => pair.Key is not "capability" and not "deviceId")
                                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
                            var intent = new CommandIntent(
                                Guid.NewGuid(),
                                executionId,
                                Guid.NewGuid(),
                                "simulator",
                                capability,
                                System.Text.Json.JsonSerializer.Serialize(parameters),
                                canonical.ActorSid,
                                canonical.ActorSid,
                                $"canonical-experiment:{experiment.ExperimentId}",
                                canonical.PolicyRevision,
                                DateTimeOffset.UtcNow);
                            var evidence = await dispatcher.ProposeAsync(intent, token).ConfigureAwait(false);
                            if (evidence.State != CommandAdmissionState.Allowed)
                            {
                                emitter?.Publish(new StepFaultedEvent(runspaceId, node.NodeId, "Command admission did not allow dispatch.")
                                {
                                    ExperimentId = experiment.ExperimentId,
                                    ExperimentPhase = experiment.ActivePhase?.ToString().ToLowerInvariant()
                                });
                                return;
                            }

                            // DispatchAsync owns the fresh admission, persistent
                            // start fence, observed outcome, and uncertain recovery.
                            await dispatcher.DispatchAsync(intent.CommandId, token).ConfigureAwait(false);
                        }
                        else if (node.NodeType.Equals("Delay", StringComparison.OrdinalIgnoreCase))
                        {
                            var duration = node.Parameters.TryGetValue("durationMin", out var value) &&
                                           value is System.Text.Json.JsonElement element &&
                                           element.ValueKind == System.Text.Json.JsonValueKind.Number
                                ? TimeSpan.FromMinutes(element.GetDouble())
                                : TimeSpan.FromMinutes(1);
                            await Task.Delay(duration, token).ConfigureAwait(false);
                        }

                        if (isStep)
                        {
                            emitter?.Publish(new StepCompletedEvent(runspaceId, node.NodeId)
                            {
                                ExperimentId = experiment.ExperimentId,
                                ExperimentPhase = experiment.ActivePhase?.ToString().ToLowerInvariant()
                            });
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        executionLogger.LogError(exception, "Step {NodeId} faulted", node.NodeId);
                        emitter?.Publish(new StepFaultedEvent(runspaceId, node.NodeId, exception.Message)
                        {
                            ExperimentId = experiment.ExperimentId,
                            ExperimentPhase = experiment.ActivePhase?.ToString().ToLowerInvariant()
                        });
                        return;
                    }

                    foreach (var edge in executionEdges.Where(edge => edge.SourceNodeId == node.NodeId))
                    {
                        var next = definition.Nodes.FirstOrDefault(candidate => candidate.NodeId == edge.TargetNodeId);
                        if (next is not null && !visited.Contains(next.NodeId))
                            queue.Enqueue(next);
                    }
                }

                emitter?.Publish(new RunspaceCompletedEvent(runspaceId)
                {
                    ExperimentId = experiment.ExperimentId,
                    ExperimentPhase = experiment.ActivePhase?.ToString().ToLowerInvariant()
                });
            };

            return await Task.FromResult<IRunspace>(runspace).ConfigureAwait(false);
        });
    });


    // WSL compilation only advertises the simulator's canonical vocabulary.
    builder.Services.AddSingleton<IWslCompiler, WslCompiler>();
    builder.Services.AddSingleton<DeviceRegistry>(sp =>
    {
        var simulator = sp.GetRequiredService<Helix.Orchestration.Devices.Drivers.SimulatorDevice>();
        var registry = new DeviceRegistry();
        registry.Register(new DeviceDescriptor(
            "simulator",
            "simulator",
            "simulator",
            simulator.GetRichCapabilities().Select(capability => capability.VerbName).ToArray(),
            new Dictionary<string, object?>()));
        return registry;
    });
    // Event bus
    builder.Services.AddSingleton<IRunspaceEventEmitter, RunspaceEventEmitter>();
    builder.Services.AddSingleton<IExperimentEventChannel, ExperimentEventChannel>();
    builder.Services.AddSingleton<IExperimentManager, ExperimentManager>();

    // Infrastructure repositories (scoped, via connection string)
    builder.Services.AddHelixInfrastructure(builder.Configuration);
    builder.Services.AddSingleton<DesktopSessionService>();
    builder.Services.AddHostedService<SessionBootstrapWorker>();
    builder.Services.AddSingleton<CommandAdmissionEvaluator>();
    builder.Services.AddSingleton<AdmissionContextProvider>();
    builder.Services.AddSingleton<ICommandDispatcher, AgentCommandDispatcher>();
    builder.Services.AddSingleton<ExecutionControlService>();
    builder.Services.AddHostedService<CommandReconciliationWorker>();

    // AgentService registered as singleton so it is resolvable as the concrete type
    // (AddHostedService<T> only registers as IHostedService, which prevents GetRequiredService<AgentService>())
    builder.Services.AddSingleton<AgentService>();
    builder.Services.AddHostedService(sp => sp.GetRequiredService<AgentService>());

    // Hosted services
    builder.Services.AddHostedService<LeaseExpiryWorker>();
    builder.Services.AddHostedService<ExperimentEventPersistenceWorker>();
    builder.Services.AddHostedService<MqttTrendIngestionService>();

    // Health checks
    builder.Services.AddHealthChecks();

    // CORS — allow Tauri webview origins (dev: Vite on 5173, prod: tauri://localhost)
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins("http://localhost:5173", "tauri://localhost", "https://tauri.localhost")
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // WebSocket support
    builder.Services.AddWebSockets(options =>
    {
        options.AllowedOrigins.Add("http://localhost:5173");
        options.AllowedOrigins.Add("tauri://localhost");
        options.AllowedOrigins.Add("https://tauri.localhost");
    });

    // gRPC
    builder.Services.AddGrpc();

    var app = builder.Build();

    app.UseCors();
    app.UseMiddleware<DesktopSessionAuthenticationHandler>();
    app.UseWebSockets();
    app.MapGrpcService<AgentGrpcService>();
    app.MapHealthChecks("/health");
    app.MapWorkflowEndpoints();
    app.MapRunspaceEndpoints();
    app.MapDeviceEndpoints();
    app.MapCommandEvidenceEndpoints();
    app.MapExperimentEndpoints();

    // WebSocket /ws endpoint
    var agentService = app.Services.GetRequiredService<AgentService>();
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/ws")
        {
            // Keep the upgrade boundary explicitly fail-closed even if middleware
            // ordering changes in a host embedding the Agent.
            if (context.Items[DesktopSessionAuthenticationHandler.ActorItemKey] is not VerifiedDesktopActor)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            if (context.WebSockets.IsWebSocketRequest)
            {
                using var ws = await context.WebSockets.AcceptWebSocketAsync();
                await agentService.HandleClientAsync(ws, context.RequestAborted);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
        else
        {
            await next(context);
        }
    });

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Helix Agent terminated unexpectedly.");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;

static void ValidateLocalOnlyListeners(IConfiguration configuration)
{
    // Configuration providers are deliberately checked before Kestrel starts.  A
    // process-level override must not silently turn the same-user Agent into a
    // lab-network service.
    var configuredUrls = new[]
    {
        configuration["ASPNETCORE_URLS"],
        configuration["urls"],
        configuration["Kestrel:Endpoints:Http:Url"]
    }
    .Where(url => !string.IsNullOrWhiteSpace(url))
    .SelectMany(url => url!.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    foreach (var configuredUrl in configuredUrls)
    {
        if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") ||
            !IsLoopbackHost(uri.Host))
        {
            throw new InvalidOperationException(
                $"Helix Agent is a same-user local service and refuses non-loopback listener '{configuredUrl}'.");
        }
    }
}

static bool IsLoopbackHost(string host) =>
    string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
    System.Net.IPAddress.TryParse(host.Trim('[', ']'), out var address) &&
    System.Net.IPAddress.IsLoopback(address);


