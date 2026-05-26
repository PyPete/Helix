using Helix.Orchestration.Devices.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.FaultTolerance;

/// <summary>
/// Background service that monitors transport health and automatically reconnects on faults.
/// </summary>
public sealed class TransportWatchdog : BackgroundService
{
    private readonly IDeviceTransport _transport;
    private readonly ReconnectPolicy _reconnectPolicy;
    private readonly ILogger<TransportWatchdog> _logger;
    private readonly TimeSpan _healthCheckInterval;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransportWatchdog"/> class.
    /// </summary>
    /// <param name="transport">The transport to monitor.</param>
    /// <param name="reconnectPolicy">The reconnection policy.</param>
    /// <param name="logger">Logger instance.</param>
    public TransportWatchdog(
        IDeviceTransport transport,
        ReconnectPolicy reconnectPolicy,
        ILogger<TransportWatchdog> logger)
        : this(transport, reconnectPolicy, logger, TimeSpan.FromSeconds(5))
    {
    }

    internal TransportWatchdog(
        IDeviceTransport transport,
        ReconnectPolicy reconnectPolicy,
        ILogger<TransportWatchdog> logger,
        TimeSpan healthCheckInterval)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _reconnectPolicy = reconnectPolicy ?? throw new ArgumentNullException(nameof(reconnectPolicy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _healthCheckInterval = healthCheckInterval;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Transport watchdog started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_healthCheckInterval, stoppingToken);

                var status = _transport.Status;

                if (status == TransportStatus.Faulted || status == TransportStatus.Disconnected)
                {
                    _logger.LogWarning("Transport is {Status}, attempting reconnection", status);
                    await AttemptReconnectAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in transport watchdog loop");
            }
        }

        _logger.LogInformation("Transport watchdog stopped");
    }

    private async Task AttemptReconnectAsync(CancellationToken cancellationToken)
    {
        var attempt = 0;
        var maxAttempts = _reconnectPolicy.MaxAttempts;

        while (!cancellationToken.IsCancellationRequested)
        {
            attempt++;

            if (maxAttempts > 0 && attempt > maxAttempts)
            {
                _logger.LogError("Reconnection failed after {MaxAttempts} attempts", maxAttempts);
                return;
            }

            try
            {
                _logger.LogInformation("Reconnection attempt {Attempt}{Max}", attempt, maxAttempts > 0 ? $"/{maxAttempts}" : "");
                await _transport.ConnectAsync(cancellationToken);

                if (_transport.Status == TransportStatus.Connected)
                {
                    _logger.LogInformation("Reconnection successful after {Attempt} attempt(s)", attempt);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reconnection attempt {Attempt} failed", attempt);
            }

            var delayMs = _reconnectPolicy.ComputeDelay(attempt);
            _logger.LogDebug("Waiting {DelayMs}ms before next attempt", delayMs);
            await Task.Delay(delayMs, cancellationToken);
        }
    }
}
