namespace Helix.Orchestration.Core.Trends;

public sealed record DerivedTrendDefinition(
    string TrendId,
    string TrendName,
    DerivationKind DerivationKind,
    string? Expression,
    IReadOnlyList<string> SourceTrendIds,
    Func<IReadOnlyDictionary<string, TrendValue>, TrendValue>? Delegate
);
