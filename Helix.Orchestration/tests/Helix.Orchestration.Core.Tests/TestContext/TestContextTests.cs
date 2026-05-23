using Helix.Orchestration.Core.Experiment;
using Helix.Orchestration.Core.TestContext;
using Helix.Orchestration.Core.Trends;
using Xunit;

namespace Helix.Orchestration.Core.Tests.TestContextTests;

public sealed class TestContextTests
{
    private static TestContext.TestContext Create() =>
        new("exp-001", "SampleExperiment");

    [Fact]
    public void NewTestContext_HasCreatedLifecycle()
    {
        var ctx = Create();
        Assert.Equal(ExperimentLifecycle.Created, ctx.Lifecycle);
    }

    [Fact]
    public async Task TransitionToRunning_UpdatesLifecycle()
    {
        var ctx = Create();
        await ctx.TransitionToAsync(ExperimentLifecycle.Running);
        Assert.Equal(ExperimentLifecycle.Running, ctx.Lifecycle);
    }

    [Fact]
    public void InjectScalar_RecordsEvent()
    {
        var ctx = Create();
        ctx.InjectScalar("temp-01", 25.3);

        var events = ctx.GetEvents();
        Assert.Single(events);
        Assert.Equal(TestEventKind.ScalarInjected, events[0].Kind);
        Assert.Equal("temp-01", events[0].DeviceOrTrendId);
    }

    [Fact]
    public void InjectSpectralScan_RecordsEvent()
    {
        var ctx = Create();
        var scan = new SpectralScan(
            "sp-01",
            [400, 500, 600],
            [0.1, 0.5, 0.3],
            SpectralType.NMR1D,
            "smp-1",
            DateTimeOffset.UtcNow,
            new Dictionary<string, object?>());

        ctx.InjectSpectralScan("nmr-01", scan);

        var events = ctx.GetEvents();
        Assert.Single(events);
        Assert.Equal(TestEventKind.SpectralScanInjected, events[0].Kind);
    }

    [Fact]
    public void InjectSpectralMatrix_RecordsEvent()
    {
        var ctx = Create();
        var intensity = new double[2, 2] { { 0.1, 0.2 }, { 0.3, 0.4 } };
        var matrix = new SpectralMatrix(
            "ms-01",
            [100.0, 200.0],
            [50.0, 60.0],
            intensity,
            SpectralType.MSMS,
            DateTimeOffset.UtcNow,
            new Dictionary<string, object?>());

        ctx.InjectSpectralMatrix("ms-01", matrix);

        Assert.Equal(TestEventKind.SpectralMatrixInjected, ctx.GetEvents()[0].Kind);
    }

    [Fact]
    public void RegisterVirtualDevice_RecordsEvent()
    {
        var ctx = Create();
        ctx.RegisterVirtualDevice("pump-sim-1", "PeristalticPump");

        var events = ctx.GetEvents();
        Assert.Single(events);
        Assert.Equal(TestEventKind.DeviceRegistered, events[0].Kind);
        Assert.Equal("pump-sim-1", events[0].DeviceOrTrendId);
    }

    [Fact]
    public void SimulateFault_ThrowIfFaulted_Throws()
    {
        var ctx = Create();
        ctx.SimulateFault("pump-sim-1", new InvalidOperationException("Pump blocked"));

        Assert.Throws<InvalidOperationException>(() => ctx.ThrowIfFaulted("pump-sim-1"));
    }

    [Fact]
    public void ClearFault_AllowsSubsequentCalls()
    {
        var ctx = Create();
        ctx.SimulateFault("pump-sim-1", new InvalidOperationException("Blocked"));
        ctx.ClearFault("pump-sim-1");

        // Should not throw
        ctx.ThrowIfFaulted("pump-sim-1");

        var events = ctx.GetEvents();
        Assert.Contains(events, e => e.Kind == TestEventKind.FaultCleared);
    }

    [Fact]
    public void GetEvents_ReturnsAllEventsInOrder()
    {
        var ctx = Create();
        ctx.InjectScalar("t1", 1.0);
        ctx.InjectScalar("t2", 2.0);
        ctx.RegisterVirtualDevice("d1", "Sensor");

        var events = ctx.GetEvents();
        Assert.Equal(3, events.Count);
        Assert.Equal(TestEventKind.ScalarInjected, events[0].Kind);
        Assert.Equal(TestEventKind.ScalarInjected, events[1].Kind);
        Assert.Equal(TestEventKind.DeviceRegistered, events[2].Kind);
    }
}
