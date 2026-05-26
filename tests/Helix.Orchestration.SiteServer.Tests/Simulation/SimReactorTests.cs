using Microsoft.Extensions.Logging.Abstractions;
using Helix.Orchestration.Core.Trends;
using Helix.Orchestration.Simulation;
using Helix.Orchestration.Simulation.Devices;
using Xunit;

namespace Helix.Orchestration.SiteServer.Tests.Simulation;

public class SimReactorTests
{
    [Fact]
    public void SetTemperature_UpdatesSetpoint_WithoutThrowing()
    {
        var provider = new SimulatedTrendProvider();
        var logger = NullLogger<SimReactor>.Instance;
        var reactor = new SimReactor(provider, logger);

        var ex = Record.Exception(() => reactor.SetTemperature(80.0));

        Assert.Null(ex);
    }

    [Fact]
    public async Task ExecuteAsync_RegistersTrends_AfterShortRun()
    {
        var provider = new SimulatedTrendProvider();
        var logger = NullLogger<SimReactor>.Instance;
        var reactor = new SimReactor(provider, logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1200));

        try { await reactor.StartAsync(cts.Token); } catch { }

        // Give the first tick time to run
        await Task.Delay(200, CancellationToken.None);

        var trendNames = provider.GetAllTrendNames().ToList();
        Assert.Contains("reactor.temp", trendNames);
        Assert.Contains("reactor.pressure", trendNames);
    }

    [Fact]
    public async Task ExecuteAsync_PublishesAtLeastOneTemperatureValue()
    {
        var provider = new SimulatedTrendProvider();
        var logger = NullLogger<SimReactor>.Instance;
        var reactor = new SimReactor(provider, logger);

        // Use a TCS so the test completes as soon as the first value arrives,
        // rather than relying on a fixed-duration observation window.
        var tcs = new TaskCompletionSource<TrendValue>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Run the reactor in the background; cancel after 5 s to bound the test.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        _ = reactor.StartAsync(cts.Token);

        // Wait until the trend is registered (ExecuteAsync registers it before its first tick)
        var deadline = Task.Delay(TimeSpan.FromSeconds(4), CancellationToken.None);
        while (!provider.GetAllTrendNames().Contains("reactor.temp"))
        {
            if (deadline.IsCompleted)
                throw new TimeoutException("reactor.temp trend was never registered.");
            await Task.Delay(10, CancellationToken.None);
        }

        using var sub = provider.GetAllTrends()
            .First(t => t.TrendId == "reactor.temp")
            .Values
            .Subscribe(v => tcs.TrySetResult(v));

        // Wait for first value with a generous timeout
        var winner = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(4), CancellationToken.None));

        await cts.CancelAsync();

        Assert.True(tcs.Task.IsCompletedSuccessfully, "No temperature value published within 4 seconds.");
        var scalar = Assert.IsType<ScalarValue<double>>(await tcs.Task);
        Assert.InRange(scalar.Value, -100.0, 500.0);
    }
}
