namespace Helix.Orchestration.Core.Trends;

public interface IDerivedTrend : ITrend
{
    DerivedTrendDefinition Definition { get; }
    IReadOnlyList<string> SourceTrendIds { get; }
}
