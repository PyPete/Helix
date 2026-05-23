namespace Helix.Orchestration.Core.Trends;

/// <summary>
/// Analytical functions operating on trend windows.
/// Provides moving average, slope calculation, stability detection, and threshold checks.
/// </summary>
public static class TrendFunctions
{
    /// <summary>
    /// Calculates the moving average of numeric values in the window.
    /// </summary>
    public static double Average(TrendWindow<double> window)
    {
        var values = window.GetValues();
        return values.Length > 0 ? values.Average() : 0.0;
    }

    /// <summary>
    /// Calculates the slope (rate of change) of the trend using linear regression.
    /// Returns the slope coefficient (value change per time unit).
    /// </summary>
    public static double Slope(TrendWindow<double> window)
    {
        var values = window.GetValues();
        if (values.Length < 2)
            return 0.0;

        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        int n = values.Length;

        for (int i = 0; i < n; i++)
        {
            sumX += i;
            sumY += values[i];
            sumXY += i * values[i];
            sumX2 += i * i;
        }

        double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        return slope;
    }

    /// <summary>
    /// Determines if the trend is stable (low variance, minimal slope).
    /// </summary>
    /// <param name="window">The trend window to analyze.</param>
    /// <param name="maxSlope">Maximum allowed absolute slope for stability.</param>
    /// <param name="maxStdDev">Maximum allowed standard deviation for stability.</param>
    /// <returns>True if the trend is stable within the specified thresholds.</returns>
    public static bool IsStable(TrendWindow<double> window, double maxSlope = 0.01, double maxStdDev = 1.0)
    {
        var values = window.GetValues();
        if (values.Length < 3)
            return false; // Not enough data to determine stability

        var slope = Slope(window);
        if (Math.Abs(slope) > maxSlope)
            return false;

        var avg = values.Average();
        var variance = values.Sum(v => Math.Pow(v - avg, 2)) / values.Length;
        var stdDev = Math.Sqrt(variance);

        return stdDev <= maxStdDev;
    }

    /// <summary>
    /// Checks if the latest value exceeds a threshold.
    /// </summary>
    public static bool AboveThreshold(TrendWindow<double> window, double threshold)
    {
        var latest = window.Latest();
        return latest.HasValue && latest.Value > threshold;
    }

    /// <summary>
    /// Checks if the latest value is below a threshold.
    /// </summary>
    public static bool BelowThreshold(TrendWindow<double> window, double threshold)
    {
        var latest = window.Latest();
        return latest.HasValue && latest.Value < threshold;
    }

    /// <summary>
    /// Checks if the latest value is within a range (inclusive).
    /// </summary>
    public static bool InRange(TrendWindow<double> window, double min, double max)
    {
        var latest = window.Latest();
        return latest.HasValue && latest.Value >= min && latest.Value <= max;
    }

    /// <summary>
    /// Gets the minimum value in the window.
    /// </summary>
    public static double? Min(TrendWindow<double> window)
    {
        var values = window.GetValues();
        return values.Length > 0 ? values.Min() : (double?)null;
    }

    /// <summary>
    /// Gets the maximum value in the window.
    /// </summary>
    public static double? Max(TrendWindow<double> window)
    {
        var values = window.GetValues();
        return values.Length > 0 ? values.Max() : (double?)null;
    }
}
