using Helix.Orchestration.Core.Samples;
using Xunit;

namespace Helix.Orchestration.Core.Tests.Samples;

public sealed class SampleFactsTests
{
    [Fact]
    public void Transformation_requires_distinct_existing_parents_and_new_children()
    {
        var parentId = Guid.NewGuid();
        var transformation = new SampleTransformation(
            Guid.NewGuid(),
            SampleTransformationKind.Split,
            "Split aliquot",
            [parentId, parentId],
            [parentId]);

        var result = SampleLineageValidator.Validate(transformation);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Contains("distinct", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Contains("new", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Transformation_accepts_many_distinct_parents_and_new_children_without_quantity()
    {
        var transformation = new SampleTransformation(
            Guid.NewGuid(),
            SampleTransformationKind.Pool,
            "Pool retained fractions",
            [Guid.NewGuid(), Guid.NewGuid()],
            [Guid.NewGuid(), Guid.NewGuid()]);

        var result = SampleLineageValidator.Validate(transformation);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Diagnostics));
    }

    [Fact]
    public void Transformation_requires_positive_exact_compatible_supplied_quantity()
    {
        var transformation = new SampleTransformation(
            Guid.NewGuid(),
            SampleTransformationKind.Processing,
            "Process fraction",
            [Guid.NewGuid()],
            [Guid.NewGuid()],
            new SampleQuantity(0, "mL", "mass"),
            QuantitySupplied: true);

        var result = SampleLineageValidator.Validate(transformation);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Contains("positive", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Contains("incompatible", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Transformation_does_not_infer_quantity_when_not_supplied()
    {
        var transformation = new SampleTransformation(
            Guid.NewGuid(),
            SampleTransformationKind.Split,
            "Split observation",
            [Guid.NewGuid()],
            [Guid.NewGuid()],
            new SampleQuantity(1, "mL", "volume"));

        var result = SampleLineageValidator.Validate(transformation);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Contains("explicitly", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Projection_uses_server_sequence_not_backdated_occurrence_time()
    {
        var sampleId = Guid.NewGuid();
        var identity = new SampleIdentity(sampleId, SampleOrigin.Collection, []);
        var recordedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var confirmation = new SampleEvent(Guid.NewGuid(), sampleId, SampleEventKind.Confirmed, "initial", "scientist", "hood", recordedAt, recordedAt, "scientist-a", 1);
        var move = new SampleEvent(Guid.NewGuid(), sampleId, SampleEventKind.CustodyMoved, "moved", "reviewer", "freezer", recordedAt.AddDays(-2), recordedAt.AddSeconds(1), "scientist-a", 2);

        var projection = SampleProjection.Reconstruct(identity, [confirmation, move]);

        Assert.Equal(2, projection.ServerSequence);
        Assert.Equal("freezer", projection.Location);
        Assert.Equal("moved", projection.Label);
    }

    [Fact]
    public void Correction_requires_prior_event_from_same_stream_and_preserves_identity()
    {
        var sampleId = Guid.NewGuid();
        var identity = new SampleIdentity(sampleId, SampleOrigin.Collection, []);
        var now = DateTimeOffset.UtcNow.AddMinutes(-2);
        var confirmed = new SampleEvent(Guid.NewGuid(), sampleId, SampleEventKind.Confirmed, "initial", "scientist", "hood", now, now, "scientist-a", 1);
        var invalidCorrection = new SampleEvent(Guid.NewGuid(), sampleId, SampleEventKind.Correction, "corrected", "scientist", "bench", now, now.AddSeconds(1), "scientist-a", 2, Guid.NewGuid(), "wrong location");

        Assert.Throws<InvalidOperationException>(() => SampleProjection.Reconstruct(identity, [confirmed, invalidCorrection]));
        Assert.Equal(sampleId, identity.SampleId);
        Assert.Equal(SampleOrigin.Collection, identity.Origin);
    }

    [Fact]
    public void Confirmation_rejects_future_occurrence_time()
    {
        var confirmation = new SampleConfirmation("hood", DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => SampleFactValidation.ValidateConfirmation(confirmation, DateTimeOffset.UtcNow));
    }
}
