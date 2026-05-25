using System.Text.Json;

namespace Helix.Orchestration.Devices.Abstractions;

/// <summary>
/// High-level device adapter interface that exposes capabilities to the Helix orchestration layer.
/// Adapters coordinate one or more IDeviceTransport instances and map capability invocations to device commands.
/// </summary>
public interface IHelixAdapter : IAsyncDisposable
{
    /// <summary>
    /// Gets the list of capabilities supported by this adapter.
    /// </summary>
    IReadOnlyList<CapabilityDescriptor> GetCapabilities();

    /// <summary>
    /// Invokes a named capability with the provided parameters.
    /// </summary>
    /// <param name="name">The capability name.</param>
    /// <param name="parameters">JSON element containing the capability parameters.</param>
    /// <param name="leaseId">Optional lease ID for capability locking/coordination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the capability invocation.</returns>
    Task<CapabilityInvocationResult> InvokeCapabilityAsync(
        string name,
        JsonElement parameters,
        string? leaseId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams trend samples (telemetry) from the device as they become available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of trend samples.</returns>
    IAsyncEnumerable<TrendSample> StreamTrendsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on the adapter and underlying transport(s).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if healthy, false otherwise.</returns>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a capability invocation.
/// </summary>
public sealed class CapabilityInvocationResult
{
    /// <summary>
    /// Indicates whether the invocation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Optional return value as a JSON element.
    /// </summary>
    public JsonElement? ReturnValue { get; init; }

    /// <summary>
    /// Optional error message if the invocation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static CapabilityInvocationResult Successful(JsonElement? returnValue = null) =>
        new() { Success = true, ReturnValue = returnValue };

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static CapabilityInvocationResult Failed(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Represents a single trend (telemetry) sample from a device.
/// </summary>
public sealed class TrendSample
{
    /// <summary>
    /// The name of the trend (e.g., "Temperature", "Pressure").
    /// </summary>
    public required string TrendName { get; init; }

    /// <summary>
    /// The sampled value.
    /// </summary>
    public required object Value { get; init; }

    /// <summary>
    /// The timestamp when the sample was taken (UTC).
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Optional unit of measurement.
    /// </summary>
    public string? Unit { get; init; }
}
