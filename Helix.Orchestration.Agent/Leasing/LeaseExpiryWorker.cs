using Helix.Orchestration.Core.Leasing;

namespace Helix.Orchestration.Agent.Leasing;

/// <summary>
/// Periodically purges expired device leases from the in-memory leasing service.
/// </summary>
public sealed class LeaseExpiryWorker(
    InMemoryDeviceLeasingService leasingService,
    ILogger<LeaseExpiryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Runs a single cleanup pass and returns the number of purged expired leases.
    /// </summary>
    public async Task<int> RunCleanupPassAsync(CancellationToken cancellationToken = default)
    {
        var purged = await leasingService.PurgeExpiredAsync(cancellationToken);

        if (purged > 0)
            logger.LogInformation("Purged {PurgedLeaseCount} expired leases", purged);

        return purged;
    }

    /// <summary>
    /// Executes the recurring lease cleanup loop.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCleanupPassAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }
}
