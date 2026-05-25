using Helix.Orchestration.Agent.Trends;
using Helix.Orchestration.Core.Trends;

namespace Helix.Orchestration.Agent.Streaming;

/// <summary>
/// Routes decoded trend values from streaming sources into the in-memory trend provider registry.
/// </summary>
public sealed class TrendAggregator(
    InMemoryTrendProvider trendProvider,
    ILogger<TrendAggregator> logger)
{
    /// <summary>
    /// Processes a decoded trend value and pushes it to the matching provider implementation.
    /// </summary>
    public Task ProcessAsync(string trendName, TrendValue value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trendName);
        ArgumentNullException.ThrowIfNull(value);

        cancellationToken.ThrowIfCancellationRequested();

        switch (value)
        {
            case SpectralValue spectral:
                trendProvider.Push1D(trendName, spectral.Scan);
                logger.LogDebug("Published spectral scan for trend {TrendName}", trendName);
                break;

            case SpectralMatrixValue matrix:
                trendProvider.Push2D(trendName, matrix.Matrix);
                logger.LogDebug("Published spectral matrix for trend {TrendName}", trendName);
                break;

            case StructuredResultValue { ResultType: "diode-array-slice", Value: DiodeArraySlice slice }:
                trendProvider.PushDiodeArraySlice(trendName, slice);
                logger.LogDebug("Published diode-array slice for trend {TrendName}", trendName);
                break;

            default:
                logger.LogDebug("Ignoring unsupported trend value type {TrendValueType} for trend {TrendName}", value.GetType().Name, trendName);
                break;
        }

        return Task.CompletedTask;
    }
}
