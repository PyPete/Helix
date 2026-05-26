using Helix.Orchestration.Core.Trends;
using Helix.Orchestration.Simulation;
using Xunit;

namespace Helix.Orchestration.SiteServer.Tests.Simulation;

public class SimulatedTrendProviderTests
{
    [Fact]
    public void RegisterTrend_ThenGetAllTrends_ReturnsRegisteredTrend()
    {
        var provider = new SimulatedTrendProvider();
        provider.RegisterTrend("t1", "t1", TrendKind.Scalar, units: "°C");

        var trends = provider.GetAllTrends().ToList();

        Assert.Single(trends);
        Assert.Equal("t1", trends[0].TrendId);
        Assert.Equal(TrendKind.Scalar, trends[0].TrendKind);
        Assert.Equal("°C", trends[0].Units);
    }

    [Fact]
    public void PublishValue_Scalar_DeliveredToObserver()
    {
        var provider = new SimulatedTrendProvider();
        provider.RegisterTrend("temp", "temp", TrendKind.Scalar, units: "°C");

        TrendValue? received = null;
        var trend = provider.GetAllTrends().Single();
        using var _ = trend.Values.Subscribe(v => received = v);

        provider.PublishValue("temp", new ScalarValue<double>(42.0));

        Assert.NotNull(received);
        var scalar = Assert.IsType<ScalarValue<double>>(received);
        Assert.Equal(42.0, scalar.Value);
    }

    [Fact]
    public void PublishValue_UnknownTrendId_DoesNotThrow()
    {
        var provider = new SimulatedTrendProvider();

        var ex = Record.Exception(() => provider.PublishValue("unknown", new ScalarValue<double>(1.0)));

        Assert.Null(ex);
    }

    [Fact]
    public void GetScalarTrend_ByName_ReturnsCorrectTrend()
    {
        var provider = new SimulatedTrendProvider();
        provider.RegisterTrend("reactor.temp", "reactor.temp", TrendKind.Scalar, units: "°C");

        var scalar = provider.GetScalarTrend<double>("reactor.temp");

        Assert.Equal("reactor.temp", scalar.TrendName);
    }

    [Fact]
    public void GetScalarTrend_UnknownName_ThrowsKeyNotFound()
    {
        var provider = new SimulatedTrendProvider();

        Assert.Throws<KeyNotFoundException>(() => provider.GetScalarTrend<double>("does.not.exist"));
    }

    [Fact]
    public void PublishValue_MultipleValues_WindowAccumulatesAll()
    {
        var provider = new SimulatedTrendProvider();
        provider.RegisterTrend("p", "p", TrendKind.Scalar, units: "bar");

        var trend = provider.GetScalarTrend<double>("p");

        provider.PublishValue("p", new ScalarValue<double>(1.0));
        provider.PublishValue("p", new ScalarValue<double>(2.0));
        provider.PublishValue("p", new ScalarValue<double>(3.0));

        Assert.Equal(3, trend.Window.Count);
        Assert.Equal(3.0, trend.Latest);
    }

    [Fact]
    public void GetTrendsByKind_FiltersCorrectly()
    {
        var provider = new SimulatedTrendProvider();
        provider.RegisterTrend("s1", "s1", TrendKind.Scalar);
        provider.RegisterTrend("sp1", "sp1", TrendKind.Spectral);

        var scalars = provider.GetTrendsByKind(TrendKind.Scalar).ToList();
        var spectrals = provider.GetTrendsByKind(TrendKind.Spectral).ToList();

        Assert.Single(scalars);
        Assert.Equal("s1", scalars[0].TrendId);
        Assert.Single(spectrals);
        Assert.Equal("sp1", spectrals[0].TrendId);
    }

    [Fact]
    public void GetSpectralTrend_ReceivesSpectrum1D_Via_Observable()
    {
        var provider = new SimulatedTrendProvider();
        provider.RegisterTrend("spectrometer.mir", "spectrometer.mir", TrendKind.Spectral, units: "Absorbance");

        SpectralValue? received = null;
        var trend = provider.GetSpectralTrend("spectrometer.mir");
        using var _ = trend.Values.Subscribe(v => received = v as SpectralValue);

        var spectrum = new Spectrum1D(
            TrendId: "spectrometer.mir",
            X: [1.0, 2.0],
            Y: [0.5, 0.6],
            SpectralType: SpectralType.MIR,
            Metadata: new MidIrMetadata(400, 4000, 14, 32, true, "Blackman-Harris"),
            SampleId: "s1",
            Timestamp: DateTimeOffset.UtcNow);

        provider.PublishValue("spectrometer.mir", new SpectralValue(spectrum));

        Assert.NotNull(received);
        Assert.Equal("spectrometer.mir", received!.Scan.TrendId);
    }
}
