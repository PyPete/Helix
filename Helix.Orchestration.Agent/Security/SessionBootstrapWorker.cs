using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent.Security;

/// <summary>Issues an opaque session only over a same-user Windows named pipe.</summary>
public sealed class SessionBootstrapWorker(DesktopSessionService sessions, ILogger<SessionBootstrapWorker> logger) : BackgroundService
{
    private const string PipeName = "helix-desktop-session";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            logger.LogWarning("Desktop session bootstrap is unavailable on non-Windows platforms.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
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
}
