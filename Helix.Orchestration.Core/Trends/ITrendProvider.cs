namespace Helix.Orchestration.Core.Trends;

/// <summary>
/// Defines the contract for accessing real-time trend data from MQTT, NATS, or other streaming sources.
/// </summary>
public interface ITrendProvider
{
    /// <summary>
    /// Retrieves a scalar trend window for backward compatibility.
    /// </summary>
    TrendWindow<T> GetTrend<T>(string trendName) where T : struct, IComparable<T>;

    /// <summary>
    /// Retrieves a typed scalar trend.
    /// </summary>
    IScalarTrend<T> GetScalarTrend<T>(string trendName) where T : struct, IComparable<T>;

    /// <summary>
    /// Retrieves a spectral trend.
    /// </summary>
    ISpectralTrend GetSpectralTrend(string trendName);

    /// <summary>
    /// Retrieves a diode array trend.
    /// </summary>
    IDiodeArrayTrend GetDiodeArrayTrend(string trendName);

    /// <summary>
    /// Retrieves an image trend.
    /// </summary>
    IImageTrend GetImageTrend(string trendName);

    /// <summary>
    /// Retrieves a video trend.
    /// </summary>
    IVideoTrend GetVideoTrend(string trendName);

    /// <summary>
    /// Retrieves a derived trend.
    /// </summary>
    IDerivedTrend GetDerivedTrend(string trendName);

    /// <summary>
    /// Checks if a trend is available and receiving data.
    /// </summary>
    Task<bool> IsTrendAvailableAsync(string trendName);

    /// <summary>
    /// Lists all available trend names.
    /// </summary>
    IEnumerable<string> GetAllTrendNames();

    /// <summary>
    /// Lists all available trend descriptors.
    /// </summary>
    IEnumerable<ITrend> GetAllTrends();

    /// <summary>
    /// Lists trends for a given trend kind.
    /// </summary>
    IEnumerable<ITrend> GetTrendsByKind(TrendKind kind);

    /// <summary>
    /// Lists trends for a given experiment.
    /// </summary>
    IEnumerable<ITrend> GetTrendsByExperiment(string experimentId);

    /// <summary>
    /// Registers a derived trend definition.
    /// </summary>
    void RegisterDerivedTrend(DerivedTrendDefinition definition);
}
