using Helix.Orchestration.Core.Trends;
using Xunit;

namespace Helix.Orchestration.Core.Tests.Trends;

public class TrendFunctionsTests
{
    [Fact]
    public void Average_ReturnsCorrectValue_ForPopulatedWindow()
    {
        // Arrange
        var window = new TrendWindow<double>("test.trend", 10);
        window.Add(10.0);
        window.Add(20.0);
        window.Add(30.0);

        // Act
        var average = TrendFunctions.Average(window);

        // Assert
        Assert.Equal(20.0, average, precision: 2);
    }

    [Fact]
    public void Average_ReturnsZero_ForEmptyWindow()
    {
        // Arrange
        var window = new TrendWindow<double>("test.trend", 10);

        // Act
        var average = TrendFunctions.Average(window);

        // Assert
        Assert.Equal(0.0, average);
    }

    [Fact]
    public void Slope_ReturnsPositiveValue_ForIncreasingTrend()
    {
        // Arrange
        var window = new TrendWindow<double>("test.trend", 10);
        for (int i = 0; i < 5; i++)
        {
            window.Add(i * 10.0); // 0, 10, 20, 30, 40
        }

        // Act
        var slope = TrendFunctions.Slope(window);

        // Assert
        Assert.True(slope > 0, "Slope should be positive for increasing trend");
        Assert.Equal(10.0, slope, precision: 1);
    }

    [Fact]
    public void Slope_ReturnsNegativeValue_ForDecreasingTrend()
    {
        // Arrange
        var window = new TrendWindow<double>("test.trend", 10);
        for (int i = 0; i < 5; i++)
        {
            window.Add(100.0 - i * 10.0); // 100, 90, 80, 70, 60
        }

        // Act
        var slope = TrendFunctions.Slope(window);

        // Assert
        Assert.True(slope < 0, "Slope should be negative for decreasing trend");
    }

    [Fact]
    public void IsStable_ReturnsTrue_ForStableTrend()
    {
        // Arrange
        var window = new TrendWindow<double>("test.trend", 10);
        for (int i = 0; i < 10; i++)
        {
            window.Add(80.0 + (i % 2 == 0 ? 0.1 : -0.1)); // Small oscillation around 80
        }

        // Act
        var isStable = TrendFunctions.IsStable(window, maxSlope: 0.1, maxStdDev: 0.5);

        // Assert
        Assert.True(isStable);
    }

    [Fact]
    public void IsStable_ReturnsFalse_ForVolatileTrend()
    {
        // Arrange
        var window = new TrendWindow<double>("test.trend", 10);
        for (int i = 0; i < 10; i++)
        {
            window.Add(i % 2 == 0 ? 50.0 : 100.0); // High oscillation
        }

        // Act
        var isStable = TrendFunctions.IsStable(window, maxSlope: 0.1, maxStdDev: 1.0);

        // Assert
        Assert.False(isStable);
    }

    [Fact]
    public void AboveThreshold_ReturnsTrue_WhenLatestValueAboveThreshold()
    {
        // Arrange
        var window = new TrendWindow<double>("test.trend", 10);
        window.Add(50.0);
        window.Add(60.0);
        window.Add(85.0);

        // Act
        var isAbove = TrendFunctions.AboveThreshold(window, 80.0);

        // Assert
        Assert.True(isAbove);
    }

    [Fact]
    public void AboveThreshold_ReturnsFalse_WhenLatestValueBelowThreshold()
    {
        // Arrange
        var window = new TrendWindow<double>("test.trend", 10);
        window.Add(50.0);
        window.Add(60.0);
        window.Add(70.0);

        // Act
        var isAbove = TrendFunctions.AboveThreshold(window, 80.0);

        // Assert
        Assert.False(isAbove);
    }

    [Fact]
    public void BelowThreshold_ReturnsTrue_WhenLatestValueBelowThreshold()
    {
        // Arrange
        var window = new TrendWindow<double>("test.trend", 10);
        window.Add(50.0);
        window.Add(40.0);
        window.Add(30.0);

        // Act
        var isBelow = TrendFunctions.BelowThreshold(window, 35.0);

        // Assert
        Assert.True(isBelow);
    }

    [Fact]
    public void InRange_ReturnsTrue_WhenLatestValueInRange()
    {
        // Arrange
        var window = new TrendWindow<double>("test.trend", 10);
        window.Add(75.0);

        // Act
        var inRange = TrendFunctions.InRange(window, 70.0, 80.0);

        // Assert
        Assert.True(inRange);
    }

    [Fact]
    public void InRange_ReturnsFalse_WhenLatestValueOutsideRange()
    {
        // Arrange
        var window = new TrendWindow<double>("test.trend", 10);
        window.Add(85.0);

        // Act
        var inRange = TrendFunctions.InRange(window, 70.0, 80.0);

        // Assert
        Assert.False(inRange);
    }

    [Fact]
    public void Min_ReturnsMinimumValue_FromWindow()
    {
        // Arrange
        var window = new TrendWindow<double>("test.trend", 10);
        window.Add(50.0);
        window.Add(30.0);
        window.Add(70.0);
        window.Add(20.0);

        // Act
        var min = TrendFunctions.Min(window);

        // Assert
        Assert.Equal(20.0, min);
    }

    [Fact]
    public void Max_ReturnsMaximumValue_FromWindow()
    {
        // Arrange
        var window = new TrendWindow<double>("test.trend", 10);
        window.Add(50.0);
        window.Add(90.0);
        window.Add(70.0);
        window.Add(20.0);

        // Act
        var max = TrendFunctions.Max(window);

        // Assert
        Assert.Equal(90.0, max);
    }
}
