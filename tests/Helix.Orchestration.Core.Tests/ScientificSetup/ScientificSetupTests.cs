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

    [Fact]
    public void Validate_accepts_a_physical_loop_and_rejects_incompatible_ports()
    {
        var pumpOut = Guid.NewGuid();
        var pumpIn = Guid.NewGuid();
        var vesselOut = Guid.NewGuid();
        var vesselIn = Guid.NewGuid();
        var loop = new ApparatusDefinition(Guid.NewGuid(), "Loop", [
            new(Guid.NewGuid(), "Pump", ["Transfer"], [new(pumpOut, "Fluid", "Out", "Many"), new(pumpIn, "Fluid", "In", "Many")]),
            new(Guid.NewGuid(), "Vessel", ["Source"], [new(vesselOut, "Fluid", "Out", "Many"), new(vesselIn, "Fluid", "In", "Many")])],
            [new(Guid.NewGuid(), pumpOut, vesselIn), new(Guid.NewGuid(), vesselOut, pumpIn)]);

        loop.Validate();
        Assert.Throws<ArgumentException>(() => (loop with { Connections = [new(Guid.NewGuid(), pumpOut, pumpOut)] }).Validate());
    }

    [Fact]
    public void Validate_rejects_unapproved_units_and_future_recorded_assertions()
    {
        var assertion = new ScientificAssertion(Guid.NewGuid(), "CalibrationRecord", "flow", "rate", null, null, 2m, "ML", DateTimeOffset.UtcNow, null, "certificate-1", "scientist");
        Assert.Throws<ArgumentException>(() => ScientificSetupValidator.ValidateAssertion(assertion, DateTimeOffset.UtcNow));

        var future = assertion with { Unit = "mL/min", ObservedAtUtc = DateTimeOffset.UtcNow.AddMinutes(1) };
        Assert.Throws<ArgumentException>(() => ScientificSetupValidator.ValidateAssertion(future, DateTimeOffset.UtcNow));
    }
}
