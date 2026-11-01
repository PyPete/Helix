using Helix.Orchestration.Infrastructure.Persistence;
using Xunit;

namespace Helix.Orchestration.Infrastructure.Tests.Persistence;

public sealed class PolicyReferenceTests
{
    [Fact]
    public void Immutable_policy_and_frozen_manifest_persistence_are_available()
    {
        var assembly = typeof(DbConnectionFactory).Assembly;

        Assert.NotNull(assembly.GetType("Helix.Orchestration.Infrastructure.Persistence.PostgresPolicyRevisionProvider"));
        Assert.NotNull(assembly.GetType("Helix.Orchestration.Infrastructure.Persistence.PostgresFrozenSetupRepository"));
    }
}
