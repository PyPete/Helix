using Helix.Orchestration.Core.ScientificSetup;
using Xunit;

namespace Helix.Orchestration.Core.Tests.ScientificSetup;

public sealed class PolicyReferenceTests
{
    [Fact]
    public void Policy_contract_exposes_independent_content_and_approval_authority()
    {
        var assembly = typeof(ApparatusDefinition).Assembly;

        Assert.NotNull(assembly.GetType("Helix.Orchestration.Core.ScientificSetup.PolicyRevisionEnvelope"));
        Assert.NotNull(assembly.GetType("Helix.Orchestration.Core.ScientificSetup.PolicyApprovalProvenance"));
        Assert.NotNull(assembly.GetType("Helix.Orchestration.Core.ScientificSetup.IPolicyApprovalAuthority"));
        Assert.NotNull(assembly.GetType("Helix.Orchestration.Core.ScientificSetup.IPolicyRevisionProvider"));
        Assert.NotNull(assembly.GetType("Helix.Orchestration.Core.ScientificSetup.FrozenSetupManifest"));
        Assert.NotNull(assembly.GetType("Helix.Orchestration.Core.ScientificSetup.IFrozenSetupRepository"));
    }
}
