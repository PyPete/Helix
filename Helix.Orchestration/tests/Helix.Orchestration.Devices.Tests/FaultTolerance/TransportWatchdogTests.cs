using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.FaultTolerance;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.FaultTolerance;

public sealed class TransportWatchdogTests
{
    [Fact]
    public async Task Watchdog_CallsConnectAsync_WhenTransportIsFaulted()
    {
        var transport = Substitute.For<IDeviceTransport>();
        transport.Status.Returns(TransportStatus.Faulted, TransportStatus.Connected);

        var watchdog = new TransportWatchdog(
            transport,
            new ReconnectPolicy { MaxAttempts = 1, InitialDelayMs = 10, MaxDelayMs = 10 },
            NullLogger<TransportWatchdog>.Instance,
            TimeSpan.FromMilliseconds(20));

        await watchdog.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await watchdog.StopAsync(CancellationToken.None);

        await transport.Received(1).ConnectAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Watchdog_Retries_WhenConnectFails()
    {
        var transport = Substitute.For<IDeviceTransport>();
        transport.Status.Returns(TransportStatus.Faulted);
        var callCount = 0;
        transport.ConnectAsync(Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                callCount++;
                if (callCount == 1)
                    throw new IOException("first failure");
                // Simulate successful connection on second attempt
                transport.Status.Returns(TransportStatus.Connected);
                return Task.CompletedTask;
            });

        var watchdog = new TransportWatchdog(
            transport,
            new ReconnectPolicy { MaxAttempts = 2, InitialDelayMs = 5, MaxDelayMs = 5, BackoffMultiplier = 1.0 },
            NullLogger<TransportWatchdog>.Instance,
            TimeSpan.FromMilliseconds(10));

        await watchdog.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await watchdog.StopAsync(CancellationToken.None);

        // Verify ConnectAsync was called at least twice
        Assert.True(callCount >= 2, $"Expected at least 2 ConnectAsync calls, but got {callCount}");
    }

    [Fact]
    public async Task Watchdog_StopsCleanly_WhenCancelled()
    {
        var transport = Substitute.For<IDeviceTransport>();
        transport.Status.Returns(TransportStatus.Connected);

        var watchdog = new TransportWatchdog(
            transport,
            new ReconnectPolicy { MaxAttempts = 1, InitialDelayMs = 10, MaxDelayMs = 10 },
            NullLogger<TransportWatchdog>.Instance,
            TimeSpan.FromMilliseconds(20));

        await watchdog.StartAsync(CancellationToken.None);
        await watchdog.StopAsync(CancellationToken.None);

        await transport.DidNotReceive().ConnectAsync(Arg.Any<CancellationToken>());
    }
}
