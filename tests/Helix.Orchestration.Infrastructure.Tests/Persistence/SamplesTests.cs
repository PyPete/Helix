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
}
