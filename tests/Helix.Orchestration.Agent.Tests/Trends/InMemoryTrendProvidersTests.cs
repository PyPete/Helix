using Helix.Orchestration.Agent.Streaming;
using Helix.Orchestration.Agent.Trends;
using Helix.Orchestration.Core.Trends;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Trends;

public sealed class InMemoryTrendProvidersTests
{
    [Fact]
    public void SpectralProvider_Push_UpdatesLatestAndBoundedHistory()
    {
        using var provider = new InMemorySpectralTrendProvider("spec-1");

        for (var i = 0; i < 105; i++)
        {
            provider.Push(new Spectrum1D(
                TrendId: "spec-1",
                X: [200 + i],
                Y: [0.1 + i],
                SpectralType: SpectralType.UV,
                Metadata: new GenericSpectralMetadata(new Dictionary<string, object?>()),
                SampleId: $"sample-{i}",
                Timestamp: DateTimeOffset.UtcNow.AddSeconds(i)));
        }

        Assert.NotNull(provider.LatestScan);
        Assert.Equal(100, provider.GetRecentScans(200).Count);
    }

    [Fact]
    public async Task TrendAggregator_ProcessesSpectralAndDiodeArrayValues()
    {
        using var trendProvider = new InMemoryTrendProvider();
        var aggregator = new TrendAggregator(trendProvider, NullLogger<TrendAggregator>.Instance);

        var scan = new Spectrum1D(
            TrendId: "spec-agg",
            X: [210d, 220d],
            Y: [1.0d, 2.0d],
            SpectralType: SpectralType.UV,
            Metadata: new GenericSpectralMetadata(new Dictionary<string, object?>()),
            SampleId: "s-1",
            Timestamp: DateTimeOffset.UtcNow);

        var slice = new DiodeArraySlice(
            Wavelengths: [210d, 220d],
            Absorbances: [0.25d, 0.50d],
            Timestamp: DateTimeOffset.UtcNow);

        await aggregator.ProcessAsync("spec-agg", new SpectralValue(scan));
        await aggregator.ProcessAsync("dad-agg", new StructuredResultValue("diode-array-slice", slice));

        var spectralTrend = trendProvider.GetSpectralTrend("spec-agg");
        var diodeTrend = trendProvider.GetDiodeArrayTrend("dad-agg");

        Assert.NotNull(spectralTrend.LatestScan);
        Assert.Single(diodeTrend.GetSnapshot());
    }

    [Fact]
    public void PushScalar_ThenGetScalarTrend_LatestEqualsLastPushed()
    {
        using var provider = new InMemoryTrendProvider();

        provider.PushScalar("temp", 42.5);
        provider.PushScalar("temp", 43.0);

        var trend = provider.GetScalarTrend<double>("temp");

        Assert.Equal(43.0, trend.Latest);
        Assert.Equal(2, trend.Window.Count);
    }

    [Fact]
    public void GetTrend_ReturnsSameWindowAsGetScalarTrend()
    {
        using var provider = new InMemoryTrendProvider();

        provider.PushScalar("pressure", 1.5);

        var window = provider.GetTrend<double>("pressure");
        var scalarWindow = provider.GetScalarTrend<double>("pressure").Window;

        Assert.Same(window, scalarWindow);
    }

    [Fact]
    public void GetImageTrend_ReturnsNonNull_PushSetsLatestFrame()
    {
        using var provider = new InMemoryTrendProvider();

        var imageTrend = provider.GetImageTrend("cam-1");
        Assert.NotNull(imageTrend);

        var frame = new ImageFrame("f1", "s3://bucket/f1.png", null, 640, 480, DateTimeOffset.UtcNow, new Dictionary<string, object?>());
        ((InMemoryImageTrend)imageTrend).Push(frame);

        Assert.Same(frame, imageTrend.LatestFrame);
    }

    [Fact]
    public void GetVideoTrend_ReturnsNonNull_PushSetsLatestSegment()
    {
        using var provider = new InMemoryTrendProvider();

        var videoTrend = provider.GetVideoTrend("vid-1");
        Assert.NotNull(videoTrend);

        var segment = new VideoSegment("seg1", "s3://bucket/seg1.mp4", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddSeconds(5));
        ((InMemoryVideoTrend)videoTrend).Push(segment);

        Assert.Same(segment, videoTrend.LatestSegment);
    }

    [Fact]
    public void RegisterDerivedTrend_ThenGetDerivedTrend_DefinitionMatches()
    {
        using var provider = new InMemoryTrendProvider();

        var def = new DerivedTrendDefinition("d1", "DerivedPressure", DerivationKind.MathExpression, "a+b", ["a", "b"], null);
        provider.RegisterDerivedTrend(def);

        var derived = provider.GetDerivedTrend("d1");

        Assert.Same(def, derived.Definition);
    }
}
