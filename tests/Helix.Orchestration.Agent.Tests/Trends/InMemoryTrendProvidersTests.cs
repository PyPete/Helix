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
}
