using Helix.Orchestration.Core.Samples;
using Helix.Orchestration.Infrastructure.Persistence;
using Xunit;

namespace Helix.Orchestration.Infrastructure.Tests.Persistence;

 [Collection(nameof(PostgresRepositoriesTestsCollection))]
public sealed class SamplesTests(PostgresFixture fixture)
{
    [Fact]
    public void Repository_is_scoped_sample_port_implementation()
    {
        Assert.True(typeof(ISampleRepository).IsAssignableFrom(typeof(PostgresSampleRepository)));
    }

    [Fact]
    public async Task Confirmation_and_append_reconstruct_exact_sequence()
    {
        Assert.NotNull(fixture.ConnectionFactory);
        var repository = new PostgresSampleRepository(fixture.ConnectionFactory!);
        var scope = new SampleScope("sample-scientist");
        var confirmed = await repository.ConfirmAsync(new SampleConfirmation("hood", DateTimeOffset.UtcNow.AddMinutes(-2), "initial", "scientist"), scope, Guid.NewGuid());
        var moved = new SampleEvent(Guid.NewGuid(), confirmed.Projection.Identity.SampleId, SampleEventKind.CustodyMoved, "moved", "reviewer", "freezer", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow, "sample-scientist", 2);

        var appended = await repository.AppendAsync(confirmed.Projection.Identity.SampleId, moved, 1, scope, Guid.NewGuid());

        Assert.Equal(2, appended.Projection.ServerSequence);
        Assert.Equal("freezer", appended.Projection.Location);
    }

    [Fact]
    public async Task Transformation_commits_new_children_edges_and_receipt_atomically()
    {
        Assert.NotNull(fixture.ConnectionFactory);
        var repository = new PostgresSampleRepository(fixture.ConnectionFactory!);
        var scope = new SampleScope("lineage-scientist");
        var parent = await repository.ConfirmAsync(new SampleConfirmation("hood", DateTimeOffset.UtcNow.AddMinutes(-1)), scope, Guid.NewGuid());
        var transformation = new SampleTransformation(
            Guid.NewGuid(),
            SampleTransformationKind.Split,
            "Split retained aliquots",
            [parent.Projection.Identity.SampleId],
            [Guid.NewGuid(), Guid.NewGuid()],
            new SampleQuantity(2.5m, "mL", "volume"),
            QuantitySupplied: true);
        var operationId = Guid.NewGuid();

        var committed = await repository.TransformAsync(transformation, scope, operationId);
        var replay = await repository.TransformAsync(transformation, scope, operationId);
        var rehydrated = await repository.ReadAsync(committed.Children[0].Identity.SampleId, scope);
        var lineage = await repository.LineageAsync(parent.Projection.Identity.SampleId, SampleLineageDirection.Descendants, scope);

        Assert.Equal("sample.transform", committed.Receipt.Kind);
        Assert.Equal(2, committed.Children.Count);
        Assert.All(committed.Children, child => Assert.Equal(SampleOrigin.Transformation, child.Identity.Origin));
        Assert.NotNull(rehydrated);
        Assert.Equal(SampleOrigin.Transformation, rehydrated!.Identity.Origin);
        Assert.Equal(parent.Projection.Identity.SampleId, Assert.Single(rehydrated.Identity.ParentSampleIds));
        Assert.Equal(committed.Children.Select(child => child.Identity.SampleId).Order(), replay.Children.Select(child => child.Identity.SampleId).Order());
        Assert.Equal(committed.Children.Select(child => child.Identity.SampleId).Order(), lineage.Entries.Select(entry => entry.SampleId).Order());
        Assert.False(lineage.IsTruncated);
    }
}
