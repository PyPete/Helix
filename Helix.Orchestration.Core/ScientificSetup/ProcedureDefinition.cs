using Helix.Orchestration.Core.WSL;

namespace Helix.Orchestration.Core.ScientificSetup;

/// <summary>Revision-owned procedure content. It describes scientific intent and never dispatches equipment.</summary>
public sealed record ProcedureDefinition(
    Guid ProcedureId,
    string Name,
    WslDocument Document,
    IReadOnlyList<ProcedureParameterDeclaration> Parameters,
    IReadOnlyList<PlannedCollectionDescriptor>? PlannedCollections = null,
    Guid? SourceRevisionId = null)
{
    public void Validate() => ScientificSetupValidator.Validate(this);
}

/// <summary>Declares an exact, explicitly addressed numeric value supplied by a plan.</summary>
public sealed record ProcedureParameterDeclaration(
    string Name,
    string Dimension,
    bool Required,
    decimal? LowerBound,
    decimal? UpperBound,
    string Unit,
    IReadOnlyList<ProcedureParameterTarget> Targets);

/// <summary>A stable WSL node and parameter key. There are no inferred paths or substitutions.</summary>
public sealed record ProcedureParameterTarget(string NodeId, string ParameterKey);

/// <summary>A planned collection is intent only; it does not create a sample fact.</summary>
public sealed record PlannedCollectionDescriptor(string NodeId, string Name);
