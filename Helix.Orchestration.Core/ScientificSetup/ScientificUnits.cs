namespace Helix.Orchestration.Core.ScientificSetup;

/// <summary>Approved, exact scientific units. This catalog performs no conversion.</summary>
public static class ScientificUnits
{
    private static readonly IReadOnlyDictionary<string, string> Dimensions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["1"] = "dimensionless", ["mL"] = "volume", ["L"] = "volume", ["g"] = "mass", ["mg"] = "mass",
        ["s"] = "time", ["min"] = "time", ["Cel"] = "temperature", ["kPa"] = "pressure", ["mL/min"] = "volume-rate"
    };

    public static string DimensionFor(string unit) => Dimensions.TryGetValue(unit, out var dimension)
        ? dimension
        : throw new ArgumentException($"'{unit}' is not an approved exact scientific unit.", nameof(unit));
}

/// <summary>A revision-owned observation, never a live calibration, safety, or dispatch authorization.</summary>
public sealed record ScientificAssertion(
    Guid AssertionId,
    string Kind,
    string Capability,
    string Parameter,
    decimal? LowerBound,
    decimal? UpperBound,
    decimal? MeasuredValue,
    string? Unit,
    DateTimeOffset ObservedAtUtc,
    DateTimeOffset? ValidThroughUtc,
    string SourceReference,
    string RecordedBy);
