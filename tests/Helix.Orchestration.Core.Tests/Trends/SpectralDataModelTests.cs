using Helix.Orchestration.Agent.Trends;
using Helix.Orchestration.Core.Trends;
using Xunit;

namespace Helix.Orchestration.Core.Tests.Trends;

public sealed class SpectralDataModelTests
{
    [Fact]
    public void Nmr1D_ProtonSpectrum_UsesTypedMetadata()
    {
        var spectrum = new Spectrum1D(
            TrendId: "nmr.1h",
            X: [10.1, 9.8, 7.2],
            Y: [0.05, 0.12, 0.22],
            SpectralType: SpectralType.NMR1D,
            Metadata: new NmrMetadata(
                Nucleus: "1H",
                FieldStrengthMhz: 600,
                Solvent: "CDCl3",
                NumberOfScans: 16,
                SpectralWidthHz: 12000,
                TransmitterFrequencyHz: 600_130_000),
            SampleId: "sample-a",
            Timestamp: DateTimeOffset.UtcNow);

        var md = Assert.IsType<NmrMetadata>(spectrum.Metadata);
        Assert.Equal("1H", md.Nucleus);
        Assert.Equal(600, md.FieldStrengthMhz);
    }

    [Fact]
    public void Nmr1D_CarbonSpectrum_UsesTypedMetadata()
    {
        var spectrum = new Spectrum1D(
            TrendId: "nmr.13c",
            X: [210.2, 172.1, 60.4],
            Y: [0.20, 0.30, 0.50],
            SpectralType: SpectralType.NMR1D,
            Metadata: new NmrMetadata(
                Nucleus: "13C",
                FieldStrengthMhz: 150,
                Solvent: "DMSO-d6",
                NumberOfScans: 128,
                SpectralWidthHz: 25000,
                TransmitterFrequencyHz: 150_900_000),
            SampleId: "sample-b",
            Timestamp: DateTimeOffset.UtcNow);

        var md = Assert.IsType<NmrMetadata>(spectrum.Metadata);
        Assert.Equal("13C", md.Nucleus);
        Assert.Equal(128, md.NumberOfScans);
    }

    [Fact]
    public void Nmr2D_Hsqc_UsesTypedMetadata()
    {
        var spectrum = new Spectrum2D(
            TrendId: "nmr.hsqc",
            Axis1: [7.1, 7.2],
            Axis2: [120.1, 121.5],
            Intensity: new double[,] { { 0.1, 0.2 }, { 0.3, 0.4 } },
            SpectralType: SpectralType.NMR2D,
            Metadata: new Nmr2DMetadata(
                F1Nucleus: "1H",
                F2Nucleus: "13C",
                ExperimentType: "HSQC",
                FieldStrengthMhz: 600,
                NumberOfPointsF1: 256,
                NumberOfPointsF2: 512),
            Timestamp: DateTimeOffset.UtcNow);

        var md = Assert.IsType<Nmr2DMetadata>(spectrum.Metadata);
        Assert.Equal("HSQC", md.ExperimentType);
        Assert.Equal("13C", md.F2Nucleus);
    }

    [Fact]
    public void Nmr2D_Cosy_UsesTypedMetadata()
    {
        var spectrum = new Spectrum2D(
            TrendId: "nmr.cosy",
            Axis1: [7.1, 7.2],
            Axis2: [7.1, 7.2],
            Intensity: new double[,] { { 0.1, 0.2 }, { 0.2, 0.1 } },
            SpectralType: SpectralType.NMR2D,
            Metadata: new Nmr2DMetadata(
                F1Nucleus: "1H",
                F2Nucleus: "1H",
                ExperimentType: "COSY",
                FieldStrengthMhz: 500,
                NumberOfPointsF1: 128,
                NumberOfPointsF2: 128),
            Timestamp: DateTimeOffset.UtcNow);

        var md = Assert.IsType<Nmr2DMetadata>(spectrum.Metadata);
        Assert.Equal("COSY", md.ExperimentType);
        Assert.Equal("1H", md.F1Nucleus);
        Assert.Equal("1H", md.F2Nucleus);
    }

    [Fact]
    public void Ms1_FullScan_UsesMsMetadata()
    {
        var spectrum = new Spectrum1D(
            TrendId: "ms.full",
            X: [100.1, 150.2, 300.4],
            Y: [1000, 5000, 2000],
            SpectralType: SpectralType.MS,
            Metadata: new MsMetadata(
                MsLevel: 1,
                PrecursorMz: null,
                PrecursorCharge: null,
                RetentionTimeSec: 42.5,
                FragmentationMethod: null,
                CollisionEnergy: null,
                ScanNumber: 1200),
            SampleId: "sample-c",
            Timestamp: DateTimeOffset.UtcNow);

        var md = Assert.IsType<MsMetadata>(spectrum.Metadata);
        Assert.Equal(1, md.MsLevel);
        Assert.Null(md.PrecursorMz);
    }

    [Fact]
    public void MsMs_FragmentationScan_UsesMsMsMetadata()
    {
        var spectrum = new Spectrum1D(
            TrendId: "msms.frag",
            X: [50.1, 75.2, 120.3],
            Y: [2500, 1800, 900],
            SpectralType: SpectralType.MSMS,
            Metadata: new MsMsMetadata(
                MsLevel: 2,
                PrecursorMz: 301.2,
                PrecursorCharge: 1,
                RetentionTimeSec: 65.1,
                FragmentationMethod: "HCD",
                CollisionEnergy: 30,
                ScanNumber: 1501,
                IsolationWindowMz: 301.2,
                IsolationWindowWidth: 1.2,
                ParentScanNumber: 1498),
            SampleId: "sample-d",
            Timestamp: DateTimeOffset.UtcNow);

        var md = Assert.IsType<MsMsMetadata>(spectrum.Metadata);
        Assert.Equal(2, md.MsLevel);
        Assert.Equal(301.2, md.PrecursorMz);
        Assert.Equal("HCD", md.FragmentationMethod);
    }

    [Fact]
    public void MidIr_AtrFtir_UsesTypedMetadata()
    {
        var spectrum = new Spectrum1D(
            TrendId: "midir.atr",
            X: [4000, 3800, 1600],
            Y: [0.01, 0.03, 0.2],
            SpectralType: SpectralType.MIR,
            Metadata: new MidIrMetadata(
                WavenumberMin: 400,
                WavenumberMax: 4000,
                ResolutionCm: 4,
                ScanCount: 32,
                AttenuatedTotalReflection: true,
                ApodizationFunction: "Happ-Genzel"),
            SampleId: "sample-ir",
            Timestamp: DateTimeOffset.UtcNow);

        var md = Assert.IsType<MidIrMetadata>(spectrum.Metadata);
        Assert.True(md.AttenuatedTotalReflection);
        Assert.Equal(4000, md.WavenumberMax);
    }

    [Fact]
    public void GenericMetadata_RoundTrips_ThroughEnvelope()
    {
        var fields = new Dictionary<string, object?>
        {
            ["instrument"] = "Custom-UV",
            ["integrationTimeMs"] = 25
        };

        var spectrum = new Spectrum1D(
            TrendId: "uv.custom",
            X: [200, 250, 300],
            Y: [0.2, 0.25, 0.28],
            SpectralType: SpectralType.UV,
            Metadata: new GenericSpectralMetadata(fields),
            SampleId: null,
            Timestamp: DateTimeOffset.UtcNow);

        var md = Assert.IsType<GenericSpectralMetadata>(spectrum.Metadata);
        Assert.Equal("Custom-UV", md.Fields["instrument"]);
        Assert.Equal(25, md.Fields["integrationTimeMs"]);
    }

    [Fact]
    public void InMemorySpectralTrendProvider_Pushes_AndEmits_Spectrum1D()
    {
        var provider = new InMemorySpectralTrendProvider("trend-1");
        Spectrum1D? observed = null;

        using var sub = provider.Scans.Subscribe(scan => observed = scan);

        var scan = new Spectrum1D(
            TrendId: "trend-1",
            X: [1, 2, 3],
            Y: [4, 5, 6],
            SpectralType: SpectralType.NMR1D,
            Metadata: new NmrMetadata("1H", 400, null, null, null, null),
            SampleId: "sample-x",
            Timestamp: DateTimeOffset.UtcNow);

        provider.Push(scan);

        Assert.NotNull(observed);
        Assert.Equal("trend-1", provider.LatestScan?.TrendId);
        Assert.Equal(SpectralType.NMR1D, observed!.SpectralType);
    }

    [Fact]
    public void InMemorySpectralTrendProvider_Pushes_AndEmits_Spectrum2D()
    {
        var provider = new InMemorySpectralTrendProvider("trend-2");
        Spectrum2D? observed = null;

        using var sub = provider.Matrices.Subscribe(matrix => observed = matrix);

        var matrix = new Spectrum2D(
            TrendId: "trend-2",
            Axis1: [1, 2],
            Axis2: [3, 4],
            Intensity: new double[,] { { 0.1, 0.2 }, { 0.3, 0.4 } },
            SpectralType: SpectralType.NMR2D,
            Metadata: new Nmr2DMetadata("1H", "13C", "HSQC", 500, 2, 2),
            Timestamp: DateTimeOffset.UtcNow);

        provider.Push(matrix);

        Assert.NotNull(observed);
        Assert.Equal("trend-2", provider.LatestMatrix?.TrendId);
        Assert.Equal(SpectralType.NMR2D, observed!.SpectralType);
    }
}
