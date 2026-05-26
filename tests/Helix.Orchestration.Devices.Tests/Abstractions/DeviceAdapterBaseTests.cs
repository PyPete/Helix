using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Abstractions;

public sealed class DeviceAdapterBaseTests
{
    [Fact]
    public async Task InvokeCapabilityAsync_DispatchesRegisteredCapability()
    {
        var transport = CreateTransport(TransportStatus.Connected);
        var adapter = new TestAdapter(transport);
        using var document = JsonDocument.Parse("{\"value\": 5}");

        var result = await adapter.InvokeCapabilityAsync("Echo", document.RootElement, "lease-1");

        Assert.True(result.Success);
        Assert.Equal("lease-1", adapter.LastLeaseId);
        Assert.Equal(5, adapter.LastValue);
    }

    [Fact]
    public async Task InvokeCapabilityAsync_UnknownCapability_ReturnsFailure()
    {
        var transport = CreateTransport(TransportStatus.Connected);
        var adapter = new TestAdapter(transport);
        using var document = JsonDocument.Parse("{}");

        var result = await adapter.InvokeCapabilityAsync("Missing", document.RootElement);

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task HealthCheckAsync_ReturnsFalse_WhenTransportIsFaulted()
    {
        var transport = CreateTransport(TransportStatus.Faulted);
        var adapter = new TestAdapter(transport);

        var healthy = await adapter.HealthCheckAsync();

        Assert.False(healthy);
    }

    [Fact]
    public async Task StreamTrendsAsync_DefaultImplementation_CompletesWithoutItems()
    {
        var transport = CreateTransport(TransportStatus.Connected);
        var adapter = new TestAdapter(transport);
        var samples = new List<TrendSample>();

        await foreach (var sample in adapter.StreamTrendsAsync(CancellationToken.None))
        {
            samples.Add(sample);
        }

        Assert.Empty(samples);
    }

    private static IDeviceTransport CreateTransport(TransportStatus status)
    {
        var transport = Substitute.For<IDeviceTransport>();
        transport.Status.Returns(status);
        return transport;
    }

    private sealed class TestAdapter : DeviceAdapterBase
    {
        public TestAdapter(IDeviceTransport transport)
            : base(transport, NullLogger.Instance)
        {
        }

        public string? LastLeaseId { get; private set; }

        public int? LastValue { get; private set; }

        protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
        {
            builder.Register(
                CapabilityDescriptor.Create("Echo", "Echoes an integer value."),
                (parameters, leaseId, cancellationToken) =>
                {
                    LastLeaseId = leaseId;
                    LastValue = parameters.GetProperty("value").GetInt32();
                    return Task.FromResult(CapabilityInvocationResult.Successful());
                });
        }
    }
}
