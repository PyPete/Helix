using Helix.Orchestration.Core.Leasing;
using Xunit;

namespace Helix.Orchestration.Core.Tests.Leasing;

public sealed class DeviceLeasingTests
{
    private static InMemoryDeviceLeasingService CreateService() => new();

    [Fact]
    public async Task AcquireExclusive_WhenFree_ReturnsLease()
    {
        var svc = CreateService();

        await using var lease = await svc.AcquireAsync("device-1", "workflow-A");

        Assert.Equal("device-1", lease.DeviceId);
        Assert.Equal("workflow-A", lease.HolderName);
        Assert.Equal(LeaseMode.Exclusive, lease.Mode);
    }

    [Fact]
    public async Task AcquireExclusive_WhenAlreadyHeld_Throws()
    {
        var svc = CreateService();
        await using var first = await svc.AcquireAsync("device-1", "workflow-A");

        await Assert.ThrowsAsync<DeviceAlreadyLeasedException>(
            () => svc.AcquireAsync("device-1", "workflow-B"));
    }

    [Fact]
    public async Task AcquireShared_TwoConcurrent_BothSucceed()
    {
        var svc = CreateService();

        await using var l1 = await svc.AcquireAsync("device-1", "workflow-A", LeaseMode.Shared);
        await using var l2 = await svc.AcquireAsync("device-1", "workflow-B", LeaseMode.Shared);

        Assert.Equal(2, svc.GetActiveLeases("device-1").Count);
    }

    [Fact]
    public async Task AcquireExclusive_AfterShared_Throws()
    {
        var svc = CreateService();
        await using var shared = await svc.AcquireAsync("device-1", "workflow-A", LeaseMode.Shared);

        await Assert.ThrowsAsync<DeviceAlreadyLeasedException>(
            () => svc.AcquireAsync("device-1", "workflow-B", LeaseMode.Exclusive));
    }

    [Fact]
    public async Task DisposeLeases_ReleasesDevice()
    {
        var svc = CreateService();
        var lease = await svc.AcquireAsync("device-1", "workflow-A");
        await lease.DisposeAsync();

        Assert.Empty(svc.GetActiveLeases("device-1"));
    }

    [Fact]
    public async Task IsHeldBy_ReturnsTrueForActiveHolder()
    {
        var svc = CreateService();
        await using var lease = await svc.AcquireAsync("device-1", "workflow-A");

        Assert.True(svc.IsHeldBy("device-1", "workflow-A"));
        Assert.False(svc.IsHeldBy("device-1", "workflow-B"));
    }

    [Fact]
    public async Task ForceRelease_ClearsAllLeases()
    {
        var svc = CreateService();
        await using var l1 = await svc.AcquireAsync("device-1", "workflow-A", LeaseMode.Shared);
        // grab a second shared lease manually without dispose
        var l2 = await svc.AcquireAsync("device-1", "workflow-B", LeaseMode.Shared);

        await svc.ForceReleaseAsync("device-1");

        Assert.Empty(svc.GetActiveLeases("device-1"));
        // Prevent double-dispose complaint by not awaiting l2 dispose after force
    }

    [Fact]
    public async Task ExpiredLease_IsNotReturnedAndAllowsReacquire()
    {
        var svc = CreateService();
        // Acquire with a 1 ms TTL and let it expire
        var lease = await svc.AcquireAsync("device-1", "workflow-A", ttl: TimeSpan.FromMilliseconds(1));
        await Task.Delay(10);

        // Should succeed because the expired lease is pruned
        await using var newLease = await svc.AcquireAsync("device-1", "workflow-B");

        Assert.Equal("workflow-B", newLease.HolderName);
    }
}
