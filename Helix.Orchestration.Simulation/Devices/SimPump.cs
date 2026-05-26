using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Helix.Orchestration.Core.Trends;

namespace Helix.Orchestration.Simulation.Devices;

/// <summary>
/// Simulated pump that produces a sine-wave flow rate variation around a baseline.
/// </summary>
public sealed class SimPump : BackgroundService
{
    private readonly SimulatedTrendProvider _trendProvider;
    private readonly ILogger<SimPump> _logger;
    private readonly Random _random = new();

    public SimPump(SimulatedTrendProvider trendProvider, ILogger<SimPump> logger)
    {
        _trendProvider = trendProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Register trend
        _trendProvider.RegisterTrend("pump.flow", "pump.flow", TrendKind.Scalar, units: "mL/min", experimentId: "sim-experiment-1");

        _logger.LogInformation("SimPump: Started. Generating sine-wave flow variation.");

        var tickInterval = TimeSpan.FromSeconds(1); // 1 Hz
        var time = 0.0;
        var baselineFlow = 10.0; // mL/min

        while (!stoppingToken.IsCancellationRequested)
        {
            // Sine wave variation around baseline + small noise
            var flow = baselineFlow + 2.0 * Math.Sin(time * 0.5) + (_random.NextDouble() - 0.5) * 0.3;

            _trendProvider.PublishValue("pump.flow", new ScalarValue<double>(flow));

            _logger.LogTrace("SimPump: Flow={Flow:F2} mL/min", flow);

            time += 0.1;
            await Task.Delay(tickInterval, stoppingToken);
        }

        _logger.LogInformation("SimPump: Stopped.");
    }
}
