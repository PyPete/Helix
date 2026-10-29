using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using Helix.Orchestration.Core.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent.Security;

/// <summary>Drains protected denial evidence before serving desktop sessions and issues sessions over a same-user pipe.</summary>
public sealed class SessionBootstrapWorker(DesktopSessionService sessions, IServiceScopeFactory scopeFactory, ILogger<SessionBootstrapWorker> logger) : BackgroundService
{
    private const string PipeName = "helix-desktop-session";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await DrainAsync(stoppingToken);
        if (!OperatingSystem.IsWindows())
        {
            logger.LogWarning("Desktop session bootstrap is unavailable on non-Windows platforms.");
            return;
        }

        var drainTask = DrainPeriodicallyAsync(stoppingToken);
        try
        {
            await ServePipeAsync(stoppingToken);
        }
        finally
        {
            await drainTask;
        }
    }

    [SupportedOSPlatform("windows")]
    private async Task ServePipeAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!sessions.IsAdmissionOpen)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                continue;
            }

            await using var pipe = new NamedPipeServerStream(PipeName, PipeDirection.Out, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly | PipeOptions.FirstPipeInstance);
            await pipe.WaitForConnectionAsync(stoppingToken);
            var identity = WindowsIdentity.GetCurrent();
            var sid = identity.User?.Value;
            if (string.IsNullOrWhiteSpace(sid))
                continue;

            var issue = sessions.Issue(sid, identity.Name ?? sid, TimeSpan.FromMinutes(15));
            var bytes = Encoding.UTF8.GetBytes($"{issue.Token}\n{issue.Reference}\n");
            await pipe.WriteAsync(bytes, stoppingToken);
            await pipe.FlushAsync(stoppingToken);
        }
    }

    private async Task DrainPeriodicallyAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await DrainAsync(stoppingToken);
    }

    private async Task DrainAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var ledger = scope.ServiceProvider.GetRequiredService<ICommandLedger>();
        var result = await sessions.DrainAsync(ledger, cancellationToken);
        if (!result.AdmissionOpen)
            logger.LogWarning("Desktop command admission remains closed while {Pending} denied-attempt records await durable history.", result.Pending);
    }
}
