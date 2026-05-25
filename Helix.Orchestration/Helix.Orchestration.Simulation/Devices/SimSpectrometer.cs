using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Helix.Orchestration.Core.Trends;

namespace Helix.Orchestration.Simulation.Devices;

/// <summary>
/// Simulated MIR spectrometer that emits Spectrum1D with Gaussian peaks every 5 seconds.
/// </summary>
public sealed class SimSpectrometer : BackgroundService
{
    private readonly SimulatedTrendProvider _trendProvider;
    private readonly ILogger<SimSpectrometer> _logger;
    private readonly Random _random = new();

    public SimSpectrometer(SimulatedTrendProvider trendProvider, ILogger<SimSpectrometer> logger)
    {
        _trendProvider = trendProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Register spectral trend
        _trendProvider.RegisterTrend(
            "spectrometer.mir", 
            "spectrometer.mir", 
            TrendKind.Spectral, 
            units: "Absorbance", 
            experimentId: "sim-experiment-1");

        _logger.LogInformation("SimSpectrometer: Started. Emitting MidIr Spectrum1D every 5 seconds.");

        var tickInterval = TimeSpan.FromSeconds(5);

        while (!stoppingToken.IsCancellationRequested)
        {
            var spectrum = GenerateGaussianSpectrum();
            var spectralValue = new SpectralValue(spectrum);
            _trendProvider.PublishValue("spectrometer.mir", spectralValue);

            _logger.LogDebug("SimSpectrometer: Emitted Spectrum1D with {Points} points.", spectrum.X.Length);

            await Task.Delay(tickInterval, stoppingToken);
        }

        _logger.LogInformation("SimSpectrometer: Stopped.");
    }

    private Spectrum1D GenerateGaussianSpectrum()
    {
        const int points = 256;
        const double minWavenumber = 400.0;  // cm⁻¹
        const double maxWavenumber = 4000.0; // cm⁻¹

        var x = new double[points];
        var y = new double[points];

        // Generate X axis (wavenumber scale)
        for (int i = 0; i < points; i++)
        {
            x[i] = minWavenumber + (maxWavenumber - minWavenumber) * i / (points - 1);
        }

        // Generate Y axis: sum of 3 Gaussian peaks + noise
        var peaks = new[]
        {
            (Center: 1750.0, Amplitude: 0.8, Width: 50.0),  // Carbonyl stretch
            (Center: 2900.0, Amplitude: 0.6, Width: 80.0),  // C-H stretch
            (Center: 3400.0, Amplitude: 0.5, Width: 100.0)  // O-H stretch
        };

        for (int i = 0; i < points; i++)
        {
            double absorbance = 0.0;
            foreach (var peak in peaks)
            {
                // Gaussian: A * exp(-((x - center)^2) / (2 * width^2))
                var exponent = -Math.Pow(x[i] - peak.Center, 2) / (2 * Math.Pow(peak.Width, 2));
                absorbance += peak.Amplitude * Math.Exp(exponent);
            }

            // Add small noise
            absorbance += (_random.NextDouble() - 0.5) * 0.02;

            y[i] = Math.Max(0, absorbance); // Absorbance cannot be negative
        }

        var metadata = new MidIrMetadata(
            WavenumberMin: minWavenumber,
            WavenumberMax: maxWavenumber,
            ResolutionCm: (maxWavenumber - minWavenumber) / points,
            ScanCount: 32,
            AttenuatedTotalReflection: true,
            ApodizationFunction: "Blackman-Harris"
        );

        return new Spectrum1D(
            TrendId: "spectrometer.mir",
            X: x,
            Y: y,
            SpectralType: SpectralType.MIR,
            Metadata: metadata,
            SampleId: "sim-sample-001",
            Timestamp: DateTimeOffset.UtcNow
        );
    }
}
