using Helix.Orchestration.Agent;
using Helix.Orchestration.Agent.Leasing;
using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Core.Leasing;
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

    // Capability invoker
    builder.Services.AddSingleton<ICapabilityInvoker, NoOpCapabilityInvoker>();

    // Hosted services
    builder.Services.AddHostedService<AgentService>();
    builder.Services.AddHostedService<LeaseExpiryWorker>();

    // Health checks
    builder.Services.AddHealthChecks();

    var app = builder.Build();

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
