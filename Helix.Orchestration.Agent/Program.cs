using Cleipnir.ResilientFunctions;
using Cleipnir.ResilientFunctions.PostgreSQL;
using Cleipnir.ResilientFunctions.Storage;
using Helix.Orchestration.Agent;
using Helix.Orchestration.Agent.Adapters.Devices;
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
using Microsoft.AspNetCore.Builder;
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

    // Durable workflow execution — PostgreSQL-backed (survives process restart)
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
    builder.Services.AddSingleton<IRunspaceManager, RunspaceManager>();
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

    // Hosted services
    builder.Services.AddHostedService<AgentService>();
    builder.Services.AddHostedService<LeaseExpiryWorker>();
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

    var app = builder.Build();

    app.UseCors();
    app.MapHealthChecks("/health");

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
