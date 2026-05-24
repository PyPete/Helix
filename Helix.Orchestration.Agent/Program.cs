using Helix.Orchestration.Agent;
using Helix.Orchestration.Agent.Adapters;
using Helix.Orchestration.Agent.Configuration;
using Helix.Orchestration.Agent.gRPC.Server;
using Helix.Orchestration.Agent.HealthChecks;
using Helix.Orchestration.Agent.Leasing;
using Helix.Orchestration.Agent.Runspaces;
using Helix.Orchestration.Agent.Streaming;
using Helix.Orchestration.Agent.Telemetry;
using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Core.Leasing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .WriteTo.File(
        formatter: new RenderedCompactJsonFormatter(),
        path: "logs/helix-agent-.clef",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        shared: true)
    .CreateLogger();

builder.Services.AddSerilog(Log.Logger, dispose: true);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("Configuration/agent-config.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "HELIX_");

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(HelixTelemetry.TelemetryName))
    .WithMetrics(metrics => metrics
        .AddMeter(HelixTelemetry.TelemetryName)
        .AddRuntimeInstrumentation());

builder.Services.Configure<AgentConfiguration>(builder.Configuration);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AgentConfiguration>>().Value);

builder.Services.AddSingleton<IDeviceLeasingService, InMemoryDeviceLeasingService>();
builder.Services.AddSingleton<ICapabilityInvoker, NoOpCapabilityInvoker>();

builder.Services.AddSingleton<AdapterRegistry>();
builder.Services.AddSingleton<IAdapterDiscovery, AdapterDiscoveryService>();
builder.Services.AddSingleton<RunspaceManager>();

builder.Services.AddSingleton<TrendBuffer>();
builder.Services.AddSingleton<MqttTrendPublisher>();
builder.Services.AddSingleton<TrendAggregator>();

builder.Services.AddHealthChecks()
    .AddCheck<AgentHealthCheck>("agent")
    .AddCheck<AdapterHealthCheck>("adapters");

builder.Services.AddHostedService<AgentService>();
builder.Services.AddHostedService<AdapterHealthMonitor>();
builder.Services.AddHostedService<LeaseExpiryWorker>();
builder.Services.AddHostedService<GrpcServerHost>();

await builder.Build().RunAsync();
