using Cleipnir.ResilientFunctions;
using Cleipnir.ResilientFunctions.PostgreSQL;
using Cleipnir.ResilientFunctions.Storage;
using Helix.Orchestration.Agent;
using Helix.Orchestration.Agent.Adapters.Devices;
using Helix.Orchestration.Agent.Events;
using Helix.Orchestration.Agent.Execution;
using Helix.Orchestration.Agent.Leasing;
using Helix.Orchestration.Agent.Streaming;
using Helix.Orchestration.Agent.Trends;
using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Leasing;
using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.Safety;
using Helix.Orchestration.Core.State;
using Helix.Orchestration.Core.Trends;
using Helix.Orchestration.Core.WorkflowEngine;
using Helix.Orchestration.Agent.Api;
using Helix.Orchestration.Agent.Experiments;
using Helix.Orchestration.Infrastructure.Persistence;
using Helix.Orchestration.Agent.GrpcServer;
using Helix.Orchestration.Core.Devices;
using Helix.Orchestration.Core.WSL;
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

    // Capability invoker + migrated device adapters
    builder.Services.AddMigratedDeviceAdapters();

    // Durable workflow execution â€” PostgreSQL-backed (survives process restart)
    var connectionString = builder.Configuration.GetConnectionString("HelixOrchestration")
        ?? throw new InvalidOperationException(
            "Connection string 'HelixOrchestration' is required for Cleipnir PostgreSQL store. " +
            "Ensure docker compose is running: docker compose up -d");

    var postgresFunctionStore = new PostgreSqlFunctionStore(connectionString);
    await postgresFunctionStore.Initialize();

    builder.Services.AddSingleton<IFunctionStore>(postgresFunctionStore);
    builder.Services.AddSingleton<FunctionsRegistry>(sp =>
        new FunctionsRegistry(sp.GetRequiredService<IFunctionStore>()));
    builder.Services.AddSingleton<WorkflowRegistry>();
    builder.Services.AddSingleton<IWorkflowStateStore, WorkflowStateStore>();
    builder.Services.AddSingleton<IGlobalStateStore, InMemoryGlobalStateStore>();
    builder.Services.AddSingleton<ISafetyEngine, SafetyEngine>();
    builder.Services.AddSingleton<InMemoryTrendProvider>();
    builder.Services.AddSingleton<ITrendProvider>(sp => sp.GetRequiredService<InMemoryTrendProvider>());
    builder.Services.AddSingleton<TrendAggregator>();
    builder.Services.AddSingleton<IRunspaceManager>(sp =>
    {
        var capRegistry       = sp.GetRequiredService<CapabilityRegistry>();
        var emitter           = sp.GetService<IRunspaceEventEmitter>();
        var execLogger        = sp.GetRequiredService<ILogger<RunspaceManager>>();
        var simulator         = sp.GetService<Helix.Orchestration.Devices.Drivers.SimulatorDevice>();
        var workflowState     = sp.GetRequiredService<IWorkflowStateStore>();
        var globalState       = sp.GetRequiredService<IGlobalStateStore>();
        var capabilityInvoker = sp.GetRequiredService<ICapabilityInvoker>();

        // Build a lookup: full capability key â†’ EstimatedDurationMs
        var estimatedDurations = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
        if (simulator != null)
        {
            foreach (var desc in simulator.GetRichCapabilities())
                estimatedDurations[$"simulator.{desc.VerbName}"] = desc.EstimatedDurationMs;
        }

        return new RunspaceManager(async (definition, ct) =>
        {
            // Deferred-body pattern: captures runspaceId after InMemoryRunspace is constructed.
            Func<CancellationToken, Task>? body = null;
            var runspace = new InMemoryRunspace(definition, token => body!(token));
            var runspaceId = runspace.RunspaceId;

            body = async (token) =>
            {
                // BFS walk from entry nodes (no incoming edges).
                // Only exec edges determine execution order; data edges are ignored for sequencing.
                var execEdges = definition.Edges.Where(e =>
                    e.EdgeKind == null || string.Equals(e.EdgeKind, "exec", StringComparison.OrdinalIgnoreCase)).ToList();

                var inDegree = definition.Nodes.ToDictionary(n => n.NodeId, _ => 0);
                foreach (var edge in execEdges)
                    if (inDegree.ContainsKey(edge.TargetNodeId))
                        inDegree[edge.TargetNodeId]++;

                var queue   = new Queue<WslNode>(definition.Nodes.Where(n => inDegree[n.NodeId] == 0));
                var visited = new HashSet<string>();

                while (queue.Count > 0 && !token.IsCancellationRequested)
                {
                    await runspace.CheckPauseAsync(token);

                    var node = queue.Dequeue();
                    if (!visited.Add(node.NodeId)) continue;

                    var isStep =
                        node.NodeType.Equals("Step",         StringComparison.OrdinalIgnoreCase) ||
                        node.NodeType.Equals("DeviceAction", StringComparison.OrdinalIgnoreCase) ||
                        node.NodeType.Equals("Delay",        StringComparison.OrdinalIgnoreCase);

                    if (isStep)
                    {
                        int? eta = null;
                        if (node.NodeType.Equals("Delay", StringComparison.OrdinalIgnoreCase))
                        {
                            // Delay nodes carry durationMin; convert to ms for the ETA hint.
                            if (node.Parameters.TryGetValue("durationMin", out var dv) &&
                                dv is System.Text.Json.JsonElement jd &&
                                jd.ValueKind == System.Text.Json.JsonValueKind.Number)
                                eta = (int)(jd.GetDouble() * 60_000);
                            else
                                eta = 60_000; // default 1 minute
                        }
                        else if (node.Parameters.TryGetValue("capability", out var capForEta) &&
                                 capForEta is string capKey &&
                                 estimatedDurations.TryGetValue(capKey, out var d))
                        {
                            eta = d;
                        }
                        emitter?.Publish(new StepStartedEvent(runspaceId, node.NodeId, node.Label, eta));
                    }

                    try
                    {
                        // Substitute ${scope:key} variable references in parameters before capability invocation.
                        var substitutedParams = await VariableSubstitution.SubstituteParametersAsync(
                            new Dictionary<string, object?>(node.Parameters),
                            definition.DefinitionId,
                            workflowState,
                            globalState,
                            capabilityInvoker,
                            campaignId: null,
                            token);

                        if ((node.NodeType.Equals("Step",         StringComparison.OrdinalIgnoreCase) ||
                             node.NodeType.Equals("DeviceAction", StringComparison.OrdinalIgnoreCase)) &&
                            substitutedParams.TryGetValue("capability", out var capObj) &&
                            capObj is string capName)
                        {
                            var result = await capRegistry.InvokeAsync(capName, substitutedParams, token);
                            if (!result.Success)
                            {
                                emitter?.Publish(new StepFaultedEvent(runspaceId, node.NodeId,
                                    result.ErrorMessage ?? $"Capability '{capName}' failed."));
                                return;
                            }
                        }
                        else if (node.NodeType.Equals("Delay", StringComparison.OrdinalIgnoreCase))
                        {
                            // Parameters arrive as JsonElement after JSON deserialization.
                            var durationMin = 1.0;
                            if (substitutedParams.TryGetValue("durationMin", out var dVal) &&
                                dVal is System.Text.Json.JsonElement je &&
                                je.ValueKind == System.Text.Json.JsonValueKind.Number)
                                durationMin = je.GetDouble();
                            await Task.Delay(TimeSpan.FromMinutes(durationMin), token);
                        }

                        if (isStep)
                            emitter?.Publish(new StepCompletedEvent(runspaceId, node.NodeId));
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        execLogger.LogError(ex, "Step {NodeId} faulted", node.NodeId);
                        emitter?.Publish(new StepFaultedEvent(runspaceId, node.NodeId, ex.Message));
                        return;
                    }

                    // Only follow exec edges for control flow; data edges carry values, not sequencing.
                    foreach (var edge in definition.Edges.Where(e =>
                        e.SourceNodeId == node.NodeId &&
                        (e.EdgeKind == null || string.Equals(e.EdgeKind, "exec", StringComparison.OrdinalIgnoreCase))))
                    {
                        var next = definition.Nodes.FirstOrDefault(n => n.NodeId == edge.TargetNodeId);
                        if (next != null && !visited.Contains(next.NodeId))
                            queue.Enqueue(next);
                    }
                }

                emitter?.Publish(new RunspaceCompletedEvent(runspaceId));
            };

            return await Task.FromResult(runspace);
        });
    });
    builder.Services.AddSingleton<Func<string, WorkflowContext>>(sp => workflowId => new WorkflowContext
    {
        WorkflowId = workflowId,
        Capabilities = sp.GetRequiredService<ICapabilityInvoker>(),
        Trends = sp.GetRequiredService<ITrendProvider>(),
        Safety = sp.GetRequiredService<ISafetyEngine>(),
        State = sp.GetRequiredService<IWorkflowStateStore>(),
        Leasing = sp.GetRequiredService<IDeviceLeasingService>(),
        GlobalState = sp.GetRequiredService<IGlobalStateStore>()
    });
    builder.Services.AddSingleton<IWorkflowExecutor, WorkflowExecutor>();
    builder.Services.AddSingleton<WslExecutionAdapter>();

    // WSL compilation + device registry
    // DeviceRegistry is populated from CapabilityRegistry at first resolve (lazy factory).
    // CapabilityRegistry is registered by AddMigratedDeviceAdapters() above, so DI ordering is safe.
    builder.Services.AddSingleton<IWslCompiler, WslCompiler>();
    builder.Services.AddSingleton<DeviceRegistry>(sp =>
    {
        var capRegistry = sp.GetRequiredService<CapabilityRegistry>();
        var deviceRegistry = new DeviceRegistry();
        var prefixes = capRegistry.GetAllCapabilityNames()
            .Select(name => { var dot = name.IndexOf('.'); return dot > 0 ? name[..dot] : name; })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var prefix in prefixes)
        {
            var caps = capRegistry.GetAllCapabilityNames()
                .Where(n => { var dot = n.IndexOf('.'); return string.Equals(dot > 0 ? n[..dot] : n, prefix, StringComparison.OrdinalIgnoreCase); })
                .Select(n => { var dot = n.IndexOf('.'); return dot > 0 ? n[(dot + 1)..] : n; })
                .ToList();
            deviceRegistry.Register(new DeviceDescriptor(prefix, prefix, "device", caps, new Dictionary<string, object?>()));
        }
        return deviceRegistry;
    });

    // Event bus
    builder.Services.AddSingleton<IRunspaceEventEmitter, RunspaceEventEmitter>();
    builder.Services.AddSingleton<IExperimentEventChannel, ExperimentEventChannel>();
    builder.Services.AddSingleton<IExperimentManager, ExperimentManager>();

    // Infrastructure repositories (scoped, via connection string)
    builder.Services.AddHelixInfrastructure(builder.Configuration);

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

    // CORS â€” allow Tauri webview origins (dev: Vite on 5173, prod: tauri://localhost)
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
    app.UseWebSockets();
    app.MapGrpcService<AgentGrpcService>();
    app.MapHealthChecks("/health");
    app.MapWorkflowEndpoints();
    app.MapRunspaceEndpoints();
    app.MapDeviceEndpoints();
    app.MapExperimentEndpoints();

    // WebSocket /ws endpoint
    var agentService = app.Services.GetRequiredService<AgentService>();
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/ws")
        {
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


