namespace Helix.Orchestration.Core.Trends;

public sealed record DiodeArraySlice(
    double[] Wavelengths,
    double[] Absorbances,
    DateTimeOffset Timestamp
);
