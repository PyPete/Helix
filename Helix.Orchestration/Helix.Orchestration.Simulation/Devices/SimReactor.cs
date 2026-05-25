using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Helix.Orchestration.Core.Trends;

namespace Helix.Orchestration.Simulation.Devices;

/// <summary>
/// Simulated reactor that produces realistic temperature ramp behavior (first-order lag)
/// and synthetic pressure output. Temperature tracks toward a setpoint; SetTemperature
/// updates the target.
/// </summary>
public sealed class SimReactor : BackgroundService
{
    private readonly SimulatedTrendProvider _trendProvider;
    private readonly ILogger<SimReactor> _logger;

    private double _setpoint = 25.0; // °C
    private double _current = 25.0;  // °C
    private readonly Random _random = new();
    private readonly object _lock = new();

    public SimReactor(SimulatedTrendProvider trendProvider, ILogger<SimReactor> logger)
    {
        _trendProvider = trendProvider;
        _logger = logger;
    }

    /// <summary>
    /// Sets the temperature setpoint. The reactor will ramp toward this value.
    /// </summary>
    public void SetTemperature(double value)
    {
        lock (_lock)
        {
            _setpoint = value;
            _logger.LogInformation("SimReactor: SetTemperature called with {Setpoint}°C", value);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Register trends
        _trendProvider.RegisterTrend("reactor.temp", "reactor.temp", TrendKind.Scalar, units: "°C", experimentId: "sim-experiment-1");
        _trendProvider.RegisterTrend("reactor.pressure", "reactor.pressure", TrendKind.Scalar, units: "bar", experimentId: "sim-experiment-1");

        _logger.LogInformation("SimReactor: Started. Initial setpoint: {Setpoint}°C", _setpoint);

        var tickInterval = TimeSpan.FromSeconds(1); // 1 Hz
        var pressureTime = 0.0;

        while (!stoppingToken.IsCancellationRequested)
        {
            double currentTemp, setpoint;
            lock (_lock)
            {
                // First-order lag: move 10% of the distance toward setpoint each tick
                _current += (_setpoint - _current) * 0.1;

                // Add small noise
                _current += (_random.NextDouble() - 0.5) * 0.2;

                currentTemp = _current;
                setpoint = _setpoint;
            }

            // Publish temperature
            _trendProvider.PublishValue("reactor.temp", new ScalarValue<double>(currentTemp));

            // Publish synthetic pressure (sine wave + 1.0 bar baseline)
            var pressure = 1.0 + 0.05 * Math.Sin(pressureTime);
            _trendProvider.PublishValue("reactor.pressure", new ScalarValue<double>(pressure));
            pressureTime += 0.1;

            _logger.LogTrace("SimReactor: Temp={Temp:F2}°C (setpoint={Setpoint}°C), Pressure={Pressure:F3}bar", 
                currentTemp, setpoint, pressure);

            await Task.Delay(tickInterval, stoppingToken);
        }

        _logger.LogInformation("SimReactor: Stopped.");
    }
}
