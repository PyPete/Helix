namespace Helix.Orchestration.Core.TestContext;

/// <summary>
/// Discriminator for events recorded in a <see cref="TestContext"/> session.
/// </summary>
public enum TestEventKind
{
    ScalarInjected,
    SpectralScanInjected,
    SpectralMatrixInjected,
    DeviceRegistered,
    FaultSimulated,
    FaultCleared
}

/// <summary>
/// A single recorded event inside a <see cref="TestContext"/>.
/// </summary>
/// <param name="Kind">The kind of event.</param>
/// <param name="Timestamp">When the event occurred.</param>
/// <param name="DeviceOrTrendId">The device or trend identifier associated with the event.</param>
/// <param name="Detail">Optional extra description or payload summary.</param>
public sealed record TestEvent(
    TestEventKind Kind,
    DateTimeOffset Timestamp,
    string DeviceOrTrendId,
    string? Detail = null);
