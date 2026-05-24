using System.Diagnostics;
using Helix.Orchestration.Agent.Telemetry;
using Helix.Orchestration.Contracts.Adapters;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent.Adapters;

/// <summary>
/// gRPC proxy used by the agent runtime to communicate with an external device adapter endpoint.
/// </summary>
public sealed class AdapterProxy : IAsyncDisposable
{
    private readonly GrpcChannel _channel;
    private readonly DeviceAdapter.DeviceAdapterClient _client;
    private readonly ILogger<AdapterProxy> _logger;

    private static class LogEvents
    {
        public static readonly EventId CapabilitiesRequested = new(2000, nameof(CapabilitiesRequested));
        public static readonly EventId CapabilityInvoked = new(2001, nameof(CapabilityInvoked));
        public static readonly EventId TrendStreamStarted = new(2002, nameof(TrendStreamStarted));
        public static readonly EventId HealthCheckRequested = new(2003, nameof(HealthCheckRequested));
        public static readonly EventId ProxyDisposed = new(2004, nameof(ProxyDisposed));
    }

    /// <summary>The logical adapter identifier.</summary>
    public string AdapterId { get; }

    /// <summary>The adapter gRPC endpoint URL.</summary>
    public string Endpoint { get; }

    /// <summary>
    /// Creates a new adapter proxy instance.
    /// </summary>
    public AdapterProxy(string adapterId, string endpoint, ILogger<AdapterProxy> logger)
    {
        AdapterId = adapterId;
        Endpoint = endpoint;
        _logger = logger;
        _channel = GrpcChannel.ForAddress(endpoint);
        _client = new DeviceAdapter.DeviceAdapterClient(_channel);
    }

    /// <summary>
    /// Retrieves adapter capabilities.
    /// </summary>
    public async Task<CapabilitiesResponse> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        using var activity = HelixTelemetry.AgentActivitySource.StartActivity("adapter.get_capabilities", ActivityKind.Client);
        activity?.SetTag("helix.adapter.id", AdapterId);
        activity?.SetTag("server.address", Endpoint);

        _logger.LogInformation(LogEvents.CapabilitiesRequested, "Requesting capabilities for adapter {AdapterId} at {Endpoint}", AdapterId, Endpoint);
        return await _client.GetCapabilitiesAsync(new Empty(), cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Invokes a specific capability on the remote adapter.
    /// </summary>
    public async Task<CapabilityResponse> InvokeAsync(string capabilityName, object? parameters, string leaseId, CancellationToken cancellationToken = default)
    {
        using var activity = HelixTelemetry.AgentActivitySource.StartActivity("adapter.invoke_capability", ActivityKind.Client);
        activity?.SetTag("helix.adapter.id", AdapterId);
        activity?.SetTag("helix.capability.name", capabilityName);
        activity?.SetTag("helix.lease.id", leaseId);

        HelixTelemetry.CapabilityInvocations.Add(1,
            KeyValuePair.Create<string, object?>("adapter.id", AdapterId),
            KeyValuePair.Create<string, object?>("capability.name", capabilityName));

        _logger.LogInformation(LogEvents.CapabilityInvoked, "Invoking capability {CapabilityName} on adapter {AdapterId}", capabilityName, AdapterId);

        var request = new CapabilityRequest
        {
            CapabilityName = capabilityName,
            ParametersJson = parameters is null ? string.Empty : System.Text.Json.JsonSerializer.Serialize(parameters),
            LeaseId = leaseId
        };

        return await _client.InvokeCapabilityAsync(request, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Opens a continuous trend stream from the remote adapter.
    /// </summary>
    public async IAsyncEnumerable<TrendValue> StreamTrendsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = HelixTelemetry.AgentActivitySource.StartActivity("adapter.stream_trends", ActivityKind.Client);
        activity?.SetTag("helix.adapter.id", AdapterId);

        _logger.LogInformation(LogEvents.TrendStreamStarted, "Starting trend stream for adapter {AdapterId}", AdapterId);

        using var call = _client.StreamTrends(new Empty(), cancellationToken: cancellationToken);
        await foreach (var value in call.ResponseStream.ReadAllAsync(cancellationToken))
            yield return value;
    }

    /// <summary>
    /// Executes a health check against the remote adapter.
    /// </summary>
    public async Task<HealthStatus> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        using var activity = HelixTelemetry.AgentActivitySource.StartActivity("adapter.health_check", ActivityKind.Client);
        activity?.SetTag("helix.adapter.id", AdapterId);

        _logger.LogInformation(LogEvents.HealthCheckRequested, "Running health check for adapter {AdapterId}", AdapterId);
        return await _client.HealthCheckAsync(new Empty(), cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Disposes the underlying gRPC channel.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        _logger.LogInformation(LogEvents.ProxyDisposed, "Disposing adapter proxy for {AdapterId}", AdapterId);
        _channel.Dispose();
        return ValueTask.CompletedTask;
    }
}
