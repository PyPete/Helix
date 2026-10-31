using Helix.Orchestration.Core.ScientificSetup;
using Helix.Orchestration.Core.WSL;
using Helix.Orchestration.Infrastructure.Persistence;
using Xunit;

namespace Helix.Orchestration.Infrastructure.Tests.Persistence;

[Collection(nameof(PostgresRepositoriesTestsCollection))]
public sealed class ScientificSetupTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Procedure_and_plan_drafts_are_independent_and_replay_by_operation_identity()
    {
        Assert.True(fixture.IsAvailable, "PostgreSQL is required for scientific setup persistence tests.");
        var repository = new PostgresScientificSetupRepository(fixture.ConnectionFactory!);
        var procedure = CreateProcedure();
        var procedureResult = await repository.CreateProcedureAsync(procedure, "scientist-procedure", Guid.NewGuid());
        var publishedProcedure = await repository.PublishProcedureAsync(procedure.ProcedureId, null, "Procedure v1", "scientist-procedure", Guid.NewGuid());
        var plan = new ExperimentPlanDefinition(Guid.NewGuid(), "Plan", publishedProcedure.Revision.RevisionId, [new("temperature", 25m, "Cel")]);

        var created = await repository.CreatePlanAsync(plan, procedure, "scientist-procedure", Guid.NewGuid());
        var reloaded = await repository.ReadPlanDraftAsync(plan.PlanId, "scientist-procedure");

        Assert.Equal(procedure.ProcedureId, procedureResult.Procedure.ProcedureId);
        Assert.Equal(plan.ProcedureRevisionId, created.Plan.ProcedureRevisionId);
        Assert.Equal(plan.Parameters, reloaded.Plan.Parameters);
        var publishedPlan = await repository.PublishPlanAsync(plan.PlanId, null, "Plan v1", "scientist-procedure", Guid.NewGuid());
        Assert.NotEqual(publishedProcedure.Revision.RevisionId, publishedPlan.Revision.RevisionId);
    }

    private static ProcedureDefinition CreateProcedure() => new(Guid.NewGuid(), "Procedure", new WslDocument
    {
        Name = "Procedure",
        Nodes = [new WslNode("heat", "Heat", "Action", new Dictionary<string, object?> { ["temperature"] = 10m })]
    }, [new("temperature", "temperature", true, 0m, 100m, "Cel", [new("heat", "temperature")])]);
}
