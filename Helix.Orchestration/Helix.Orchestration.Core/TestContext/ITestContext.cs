using Helix.Orchestration.Core.Experiment;
using Helix.Orchestration.Core.Trends;

namespace Helix.Orchestration.Core.TestContext;

/// <summary>
/// Extends <see cref="IExperimentContext"/> with simulation hooks used in
/// unit tests and hardware-in-the-loop test runs.
/// </summary>
public interface ITestContext : IExperimentContext
{
    /// <summary>
    /// Injects a simulated scalar reading for a trend.
    /// The value appears as if it was received from a real device.
    /// </summary>
    void InjectScalar<T>(string trendId, T value) where T : struct;

    /// <summary>
    /// Injects a simulated spectral scan for a trend.
    /// </summary>
    void InjectSpectralScan(string trendId, SpectralScan scan);

    /// <summary>
    /// Injects a simulated spectral matrix for a trend.
    /// </summary>
    void InjectSpectralMatrix(string trendId, SpectralMatrix matrix);

    /// <summary>
    /// Registers a virtual device identifier so leasing and capability
    /// lookups work against the test context without real hardware.
    /// </summary>
    void RegisterVirtualDevice(string deviceId, string deviceType);

    /// <summary>
    /// Simulates a device fault, causing the next interaction with
    /// <paramref name="deviceId"/> to throw.
    /// </summary>
    void SimulateFault(string deviceId, Exception fault);

    /// <summary>
    /// Clears any injected faults for <paramref name="deviceId"/>.
    /// </summary>
    void ClearFault(string deviceId);

    /// <summary>
    /// Returns a log of all injections and fault events, in order.
    /// </summary>
    IReadOnlyList<TestEvent> GetEvents();
}
