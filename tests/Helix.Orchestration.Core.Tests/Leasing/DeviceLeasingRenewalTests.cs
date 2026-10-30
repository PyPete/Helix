using Helix.Orchestration.Core.Leasing;
using Xunit;

namespace Helix.Orchestration.Core.Tests.Leasing;

public sealed class DeviceLeasingRenewalTests
{
    [Fact]
    public async Task RenewAsync_ExtendsLeaseExpiration()
    {
        var service = new InMemoryDeviceLeasingService();
        await using var lease = await service.AcquireAsync(
            deviceId: "pump-1",
            holderName: "workflow-a",
            ttl: TimeSpan.FromSeconds(1));

        var originalExpiry = Assert.IsType<DateTimeOffset>(lease.ExpiresAt);

        var renewed = await service.RenewAsync("pump-1", lease.LeaseId, TimeSpan.FromSeconds(30));

        var renewedExpiry = Assert.IsType<DateTimeOffset>(renewed.ExpiresAt);
        Assert.True(renewedExpiry > originalExpiry);
    }

    [Fact]
    public async Task RenewAsync_WhenLeaseExpired_ThrowsInvalidOperationException()
    {
        var service = new InMemoryDeviceLeasingService();
        await using var lease = await service.AcquireAsync(
            deviceId: "pump-1",
            holderName: "workflow-a",
            ttl: TimeSpan.FromMilliseconds(1));

        await Task.Delay(20);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RenewAsync("pump-1", lease.LeaseId, TimeSpan.FromSeconds(5)));
    }
}
