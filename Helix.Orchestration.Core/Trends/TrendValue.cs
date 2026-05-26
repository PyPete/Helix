namespace Helix.Orchestration.Core.Trends;

public abstract record TrendValue;

public sealed record ScalarValue<T>(T Value) : TrendValue where T : struct, IComparable<T>;

public sealed record DoubleArrayValue(double[] XData, double[] YData) : TrendValue;

public sealed record SpectralValue(Spectrum1D Scan) : TrendValue;

public sealed record SpectralMatrixValue(Spectrum2D Matrix) : TrendValue;

public sealed record ImageValue(ImageFrame Frame) : TrendValue;

public sealed record VideoValue(VideoSegment Segment) : TrendValue;

public sealed record StructuredResultValue(string ResultType, object? Value) : TrendValue;
