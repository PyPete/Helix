using Helix.Orchestration.Agent.Streaming;
using Helix.Orchestration.Agent.Trends;
using Helix.Orchestration.Core.Trends;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Streaming;

public sealed class TrendAggregatorScalarTests
{
    [Fact]
    public async Task ProcessAsync_ScalarDouble_PushesToInMemoryProvider()
    {
        var provider = new InMemoryTrendProvider();
        var aggregator = new TrendAggregator(provider, NullLogger<TrendAggregator>.Instance);

        await aggregator.ProcessAsync("my.temp", new ScalarValue<double>(42.5));

        Assert.True(provider.TryGetScalar("my.temp", out var value));
        Assert.Equal(42.5, value);
    }

    [Fact]
    public async Task ProcessAsync_ScalarFloat_PushesToInMemoryProvider()
    {
        var provider = new InMemoryTrendProvider();
        var aggregator = new TrendAggregator(provider, NullLogger<TrendAggregator>.Instance);

        await aggregator.ProcessAsync("my.pressure", new ScalarValue<float>(3.14f));

        Assert.True(provider.TryGetScalar("my.pressure", out var value));
        Assert.Equal(3.14f, value);
    }

    [Fact]
    public async Task ProcessAsync_ScalarInt_PushesToInMemoryProvider()
    {
        var provider = new InMemoryTrendProvider();
        var aggregator = new TrendAggregator(provider, NullLogger<TrendAggregator>.Instance);

        await aggregator.ProcessAsync("my.count", new ScalarValue<int>(99));

        Assert.True(provider.TryGetScalar("my.count", out var value));
        Assert.Equal(99, value);
    }

    [Fact]
    public async Task ProcessAsync_UnsupportedType_DoesNotThrow()
    {
        var provider = new InMemoryTrendProvider();
        var aggregator = new TrendAggregator(provider, NullLogger<TrendAggregator>.Instance);

        // DoubleArrayValue is not handled by the switch and falls to default
        var exception = await Record.ExceptionAsync(
            () => aggregator.ProcessAsync("x", new DoubleArrayValue([1.0, 2.0], [3.0, 4.0])));

        Assert.Null(exception);
    }

    [Fact]
    public async Task ProcessAsync_IsTrendAvailable_AfterScalarPush()
    {
        var provider = new InMemoryTrendProvider();
        var aggregator = new TrendAggregator(provider, NullLogger<TrendAggregator>.Instance);

        await aggregator.ProcessAsync("sensor.temp", new ScalarValue<double>(25.0));

        Assert.True(await provider.IsTrendAvailableAsync("sensor.temp"));
    }
}