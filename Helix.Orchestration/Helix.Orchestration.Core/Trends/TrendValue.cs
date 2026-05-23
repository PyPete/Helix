namespace Helix.Orchestration.Core.Trends;

public abstract record TrendValue;

public sealed record ScalarValue<T>(T Value) : TrendValue where T : struct, IComparable<T>;

public sealed record DoubleArrayValue(double[] XData, double[] YData) : TrendValue;

public sealed record SpectralValue(SpectralScan Scan) : TrendValue;

public sealed record SpectralMatrixValue(SpectralMatrix Matrix) : TrendValue;

public sealed record ImageValue(ImageFrame Frame) : TrendValue;

public sealed record VideoValue(VideoSegment Segment) : TrendValue;

public sealed record StructuredResultValue(string ResultType, object? Value) : TrendValue;
