namespace Helix.Orchestration.Core.Trends;

public interface IPythonTrendEvaluator
{
    Task<TrendValue> EvaluateAsync(
        IReadOnlyDictionary<string, TrendValue> inputs,
        string script,
        CancellationToken cancellationToken = default
    );
}
