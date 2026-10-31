using Helix.Orchestration.Core.ScientificSetup;
using Xunit;

namespace Helix.Orchestration.Core.Tests.ScientificSetup;

public sealed class ScientificSetupTests
{
    [Fact]
    public void Validate_rejects_a_pump_without_the_required_transfer_role()
    {
        var apparatus = new ApparatusDefinition(
            Guid.NewGuid(),
            "Transfer loop",
            [new ApparatusComponent(Guid.NewGuid(), "Pump", ["Source"])]);

        var exception = Assert.Throws<ArgumentException>(apparatus.Validate);

        Assert.Contains("Pump", exception.Message, StringComparison.Ordinal);
    }
}
