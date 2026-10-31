using Helix.Orchestration.Core.ScientificSetup;
using Helix.Orchestration.Core.WSL;
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

    [Fact]
    public void Procedure_plan_validation_and_instantiation_replace_only_declared_decimal_targets()
    {
        var document = new WslDocument
        {
            DocumentId = "procedure-1",
            Name = "Procedure",
            Nodes = [new WslNode("heat", "Heat", "Action", new Dictionary<string, object?> { ["temperature"] = 10m, ["label"] = "unchanged" })]
        };
        var procedure = new ProcedureDefinition(Guid.NewGuid(), "Heat", document,
        [new("temperature", "temperature", true, -20m, 100m, "Cel", [new("heat", "temperature")])]);
        var plan = new ExperimentPlanDefinition(Guid.NewGuid(), "Plan", Guid.NewGuid(), [new("temperature", 25m, "Cel")]);

        var instantiated = plan.Instantiate(procedure);

        Assert.Equal(25m, instantiated.Nodes[0].Parameters["temperature"]);
        Assert.Equal("unchanged", instantiated.Nodes[0].Parameters["label"]);
        Assert.Equal("heat", instantiated.Nodes[0].NodeId);
        Assert.Throws<ArgumentException>(() => (plan with { Parameters = [new("temperature", 25m, "mL")] }).ValidateAgainst(procedure));
    }

    [Fact]
    public void Procedure_validation_rejects_overlapping_or_missing_targets()
    {
        var document = new WslDocument { Name = "Procedure", Nodes = [new WslNode("step", "Step", "Action", new Dictionary<string, object?> { ["amount"] = 1m })] };
        var declaration = new ProcedureParameterDeclaration("amount", "volume", true, null, null, "mL", [new("step", "amount")]);
        var overlapping = new ProcedureDefinition(Guid.NewGuid(), "P", document, [declaration, declaration with { Name = "other" }]);

        Assert.Throws<ArgumentException>(overlapping.Validate);
        Assert.Throws<ArgumentException>(() => (overlapping with { Parameters = [declaration with { Targets = [new("missing", "amount")] }] }).Validate());
    }
}
