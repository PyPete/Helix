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

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1500));

        TrendValue? received = null;

        try { await reactor.StartAsync(cts.Token); } catch { }

        // Subscribe after start so trend is registered
        await Task.Delay(100, CancellationToken.None);
        using var sub = provider.GetAllTrends()
            .First(t => t.TrendId == "reactor.temp")
            .Values
            .Subscribe(v => received = v);

        await Task.Delay(1200, CancellationToken.None);

        Assert.NotNull(received);
        var scalar = Assert.IsType<ScalarValue<double>>(received);
        Assert.InRange(scalar.Value, -100.0, 500.0);
    }
}
