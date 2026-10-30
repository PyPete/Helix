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

    AgentApplicationComposition.ConfigureServices(builder);

    var app = builder.Build();
    AgentApplicationComposition.ConfigurePipeline(app);
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


