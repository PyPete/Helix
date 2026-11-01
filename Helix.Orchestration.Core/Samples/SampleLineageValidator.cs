namespace Helix.Orchestration.Core.Samples;

public static class SampleLineageValidator
{
    private static readonly IReadOnlyDictionary<string, string> UnitDimensions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["1"] = "dimensionless",
        ["mL"] = "volume",
        ["L"] = "volume",
        ["g"] = "mass",
        ["mg"] = "mass",
        ["s"] = "time",
        ["min"] = "time",
        ["Cel"] = "temperature",
        ["kPa"] = "pressure",
        ["mL/min"] = "volume-rate"
    };

    public static SampleLineageValidationResult Validate(SampleTransformation transformation)
    {
        ArgumentNullException.ThrowIfNull(transformation);

        var diagnostics = new List<string>();
        if (transformation.TransformationId == Guid.Empty)
            diagnostics.Add("A transformation requires a stable identifier.");
        if (!Enum.IsDefined(transformation.Kind))
            diagnostics.Add("A transformation kind is required.");
        if (string.IsNullOrWhiteSpace(transformation.Operation))
            diagnostics.Add("A nonempty transformation operation is required.");

        ValidateReferences(transformation.ParentSampleIds, "parent", diagnostics);
        ValidateReferences(transformation.ChildSampleIds, "new child", diagnostics);

        if (transformation.ParentSampleIds.Intersect(transformation.ChildSampleIds).Any())
            diagnostics.Add("New children must be distinct from every parent.");

        ValidateQuantity(transformation, diagnostics);
        return new SampleLineageValidationResult(diagnostics);
    }

    private static void ValidateReferences(IReadOnlyList<Guid> identifiers, string name, ICollection<string> diagnostics)
    {
        if (identifiers.Count == 0)
            diagnostics.Add($"At least one {name} is required.");
        if (identifiers.Count > 32)
            diagnostics.Add($"At most 32 {name} references are allowed.");
        if (identifiers.Any(identifier => identifier == Guid.Empty))
            diagnostics.Add($"Every {name} requires a stable identifier.");
        if (identifiers.Distinct().Count() != identifiers.Count)
            diagnostics.Add($"Each {name} must be distinct.");
    }

    private static void ValidateQuantity(SampleTransformation transformation, ICollection<string> diagnostics)
    {
        if (!transformation.QuantitySupplied)
        {
            if (transformation.Quantity is not null)
                diagnostics.Add("A quantity must be explicitly marked as supplied.");
            return;
        }

        if (transformation.Quantity is null)
        {
            diagnostics.Add("A supplied quantity requires an exact value, unit, and dimension.");
            return;
        }

        if (transformation.Quantity.Value <= 0)
            diagnostics.Add("A supplied transformation quantity must be positive.");
        if (!UnitDimensions.TryGetValue(transformation.Quantity.UnitCode, out var dimension))
            diagnostics.Add("A supplied transformation quantity uses an unknown exact unit code.");
        else if (!StringComparer.Ordinal.Equals(dimension, transformation.Quantity.Dimension))
            diagnostics.Add("A supplied transformation quantity unit and dimension are incompatible.");
    }
}
