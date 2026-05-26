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
        using var trendProvider = new InMemoryTrendProvider();
        var aggregator = new TrendAggregator(trendProvider, NullLogger<TrendAggregator>.Instance);

        await aggregator.ProcessAsync("my.temp", new ScalarValue<double>(42.5), CancellationToken.None);

        Assert.True(trendProvider.TryGetScalar("my.temp", out var v));
        Assert.Equal(42.5, (double)v!);
    }

    [Fact]
    public async Task ProcessAsync_ScalarFloat_PushesToInMemoryProvider()
    {
        using var trendProvider = new InMemoryTrendProvider();
        var aggregator = new TrendAggregator(trendProvider, NullLogger<TrendAggregator>.Instance);

        await aggregator.ProcessAsync("my.pressure", new ScalarValue<float>(3.14f), CancellationToken.None);

        Assert.True(trendProvider.TryGetScalar("my.pressure", out var v));
        Assert.Equal(3.14f, (float)v!);
    }

    [Fact]
    public async Task ProcessAsync_UnsupportedType_DoesNotThrow()
    {
        using var trendProvider = new InMemoryTrendProvider();
        var aggregator = new TrendAggregator(trendProvider, NullLogger<TrendAggregator>.Instance);

        // DoubleArrayValue is a known TrendValue subtype not handled by the aggregator switch
        var unsupported = new DoubleArrayValue(XData: [1.0, 2.0], YData: [3.0, 4.0]);

        var ex = await Record.ExceptionAsync(() =>
            aggregator.ProcessAsync("my.unsupported", unsupported, CancellationToken.None));

        Assert.Null(ex);
    }
}
