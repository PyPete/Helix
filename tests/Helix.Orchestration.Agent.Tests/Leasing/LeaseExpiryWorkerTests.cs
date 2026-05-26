using Helix.Orchestration.Agent.Leasing;
using Helix.Orchestration.Core.Leasing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Leasing;

public sealed class LeaseExpiryWorkerTests
{
    [Fact]
    public async Task ExpiredLease_IsPurged_ByCleanupPass()
    {
        var leasing = new InMemoryDeviceLeasingService();
        var worker = new LeaseExpiryWorker(leasing, NullLogger<LeaseExpiryWorker>.Instance);

        await using var expired = await leasing.AcquireAsync(
            deviceId: "device-1",
            holderName: "runspace-a",
            ttl: TimeSpan.FromMilliseconds(1));

        await Task.Delay(20);

        await worker.RunCleanupPassAsync();

        Assert.Empty(leasing.GetActiveLeases("device-1"));
    }

    [Fact]
    public async Task ActiveLease_IsRetained_ByCleanupPass()
    {
        var leasing = new InMemoryDeviceLeasingService();
        var worker = new LeaseExpiryWorker(leasing, NullLogger<LeaseExpiryWorker>.Instance);

        await using var active = await leasing.AcquireAsync(
            deviceId: "device-1",
            holderName: "runspace-a",
            ttl: TimeSpan.FromMinutes(5));

        await worker.RunCleanupPassAsync();

        var activeLeases = leasing.GetActiveLeases("device-1");
        Assert.Single(activeLeases);
        Assert.Equal("runspace-a", activeLeases[0].HolderName);
    }
}
