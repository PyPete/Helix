using System.Threading.Channels;
using Helix.Orchestration.Agent.Trends;
using Helix.Orchestration.Core.Trends;

namespace Helix.Orchestration.Agent.Streaming;

/// <summary>
/// Routes decoded trend values from streaming sources into the in-memory trend provider registry.
/// Also publishes scalar values to <see cref="ScalarValues"/> for consumers such as AgentService
/// that want to broadcast live trend frames over WebSocket.
/// </summary>
public sealed class TrendAggregator(
    InMemoryTrendProvider trendProvider,
    ILogger<TrendAggregator> logger)
{
    private readonly Channel<ScalarTrendNotification> _scalarChannel =
        Channel.CreateUnbounded<ScalarTrendNotification>(
            new UnboundedChannelOptions { SingleReader = false, SingleWriter = true });

    /// <summary>
    /// A reader over the scalar trend notification channel.
    /// Each entry represents a scalar value that was pushed via <see cref="ProcessAsync"/>.
    /// </summary>
    public ChannelReader<ScalarTrendNotification> ScalarValues => _scalarChannel.Reader;

    /// <summary>
    /// Processes a decoded trend value and pushes it to the matching provider implementation.
    /// Scalar values are also published to <see cref="ScalarValues"/> for live broadcasting.
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

            case ScalarValue<double> scalar:
                trendProvider.PushScalar(trendName, scalar.Value);
                _scalarChannel.Writer.TryWrite(new ScalarTrendNotification(trendName, scalar.Value, string.Empty));
                logger.LogDebug("Published scalar (double) for trend {TrendName}: {Value}", trendName, scalar.Value);
                break;

            case ScalarValue<float> scalarF:
                trendProvider.PushScalar(trendName, scalarF.Value);
                _scalarChannel.Writer.TryWrite(new ScalarTrendNotification(trendName, scalarF.Value, string.Empty));
                logger.LogDebug("Published scalar (float) for trend {TrendName}: {Value}", trendName, scalarF.Value);
                break;

            case ScalarValue<int> scalarI:
                trendProvider.PushScalar(trendName, scalarI.Value);
                _scalarChannel.Writer.TryWrite(new ScalarTrendNotification(trendName, scalarI.Value, string.Empty));
                logger.LogDebug("Published scalar (int) for trend {TrendName}: {Value}", trendName, scalarI.Value);
                break;

            default:
                logger.LogWarning("Ignoring unsupported trend value type {TrendValueType} for trend {TrendName}", value.GetType().Name, trendName);
                break;
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Notification emitted by <see cref="TrendAggregator"/> when a scalar trend value is processed.
/// </summary>
/// <param name="TrendName">Name of the trend (e.g. "reactor.temperature").</param>
/// <param name="Value">Scalar value as double.</param>
/// <param name="Unit">Engineering unit. Empty if not available from the source.</param>
public sealed record ScalarTrendNotification(string TrendName, double Value, string Unit);
