using Helix.Orchestration.Core.WSL;

namespace Helix.Orchestration.Core.ScientificSetup;

/// <summary>An independently revisioned plan that pins one published procedure revision.</summary>
public sealed record ExperimentPlanDefinition(
    Guid PlanId,
    string Name,
    Guid ProcedureRevisionId,
    IReadOnlyList<ExperimentParameterValue> Parameters)
{
    public void ValidateAgainst(ProcedureDefinition procedure) => ScientificSetupValidator.Validate(this, procedure);

    /// <summary>Creates a deep WSL copy and replaces only declared targets with exact decimal values.</summary>
    public WslDocument Instantiate(ProcedureDefinition procedure)
    {
        ValidateAgainst(procedure);
        var values = Parameters.ToDictionary(value => value.Name, StringComparer.Ordinal);
        var replacements = procedure.Parameters
            .SelectMany(declaration => declaration.Targets.Select(target => new { declaration.Name, target }))
            .ToDictionary(item => (item.target.NodeId, item.target.ParameterKey), item => (object?)values[item.Name].Value);

        return procedure.Document with
        {
            Nodes = procedure.Document.Nodes.Select(node => node with
            {
                Parameters = node.Parameters.ToDictionary(
                    pair => pair.Key,
                    pair => replacements.TryGetValue((node.NodeId, pair.Key), out var value) ? value : pair.Value,
                    StringComparer.Ordinal)
            }).ToArray(),
            Edges = procedure.Document.Edges.Select(edge => edge with { }).ToArray()
        };
    }
}

/// <summary>A plan-supplied decimal and its exact declared unit.</summary>
public sealed record ExperimentParameterValue(string Name, decimal Value, string Unit);
