namespace Helix.Orchestration.Recorder;

/// <summary>
/// Background worker for the Helix Orchestration Recorder service.
/// Handles recording of experiment execution traces and events.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Helix Orchestration Recorder service started at {Time}", DateTimeOffset.Now);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Recorder service is running at {Time}", DateTimeOffset.Now);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Recorder service stopping gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recorder service encountered an error");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Helix Orchestration Recorder service stopping at {Time}", DateTimeOffset.Now);
        await base.StopAsync(cancellationToken);
    }
}
