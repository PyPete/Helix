using Helix.Orchestration.Core.Trends;
using Xunit;

namespace Helix.Orchestration.Core.Tests.Trends;

public class TrendValueTests
{
    [Fact]
    public void SpectralMatrixValue_HoldsMatrixPayload()
    {
        var matrix = new SpectralMatrix(
            TrendId: "nmr.cosy",
            Axis1: [1.1, 2.2],
            Axis2: [7.1, 7.2],
            Intensity: new double[,] { { 0.1, 0.2 }, { 0.3, 0.4 } },
            SpectralType: SpectralType.NMR2D,
            Timestamp: DateTimeOffset.UtcNow,
            Metadata: new Dictionary<string, object?>
            {
                ["ExperimentType"] = "COSY",
                ["Nucleus"] = "1H"
            }
        );

        TrendValue value = new SpectralMatrixValue(matrix);

        var typed = Assert.IsType<SpectralMatrixValue>(value);
        Assert.Equal("nmr.cosy", typed.Matrix.TrendId);
        Assert.Equal(SpectralType.NMR2D, typed.Matrix.SpectralType);
        Assert.Equal("COSY", typed.Matrix.Metadata["ExperimentType"]);
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
