using Helix.Orchestration.Core.Trends;
using Xunit;

namespace Helix.Orchestration.Core.Tests.Trends;

public class TrendValueTests
{
    [Fact]
    public void SpectralMatrixValue_HoldsMatrixPayload()
    {
        var matrix = new Spectrum2D(
            TrendId: "nmr.cosy",
            Axis1: [1.1, 2.2],
            Axis2: [7.1, 7.2],
            Intensity: new double[,] { { 0.1, 0.2 }, { 0.3, 0.4 } },
            SpectralType: SpectralType.NMR2D,
            Metadata: new Nmr2DMetadata(
                F1Nucleus: "1H",
                F2Nucleus: "1H",
                ExperimentType: "COSY",
                FieldStrengthMhz: 400,
                NumberOfPointsF1: 2,
                NumberOfPointsF2: 2),
            Timestamp: DateTimeOffset.UtcNow
        );

        TrendValue value = new SpectralMatrixValue(matrix);

        var typed = Assert.IsType<SpectralMatrixValue>(value);
        Assert.Equal("nmr.cosy", typed.Matrix.TrendId);
        Assert.Equal(SpectralType.NMR2D, typed.Matrix.SpectralType);
        var md = Assert.IsType<Nmr2DMetadata>(typed.Matrix.Metadata);
        Assert.Equal("COSY", md.ExperimentType);
    }

    [Fact]
    public void SpectralType_ContainsMsAndNmrVariants()
    {
        Assert.Contains(SpectralType.MS, Enum.GetValues<SpectralType>());
        Assert.Contains(SpectralType.MSMS, Enum.GetValues<SpectralType>());
        Assert.Contains(SpectralType.NMR1D, Enum.GetValues<SpectralType>());
        Assert.Contains(SpectralType.NMR2D, Enum.GetValues<SpectralType>());
    }
}
