namespace Helix.Orchestration.Core.Trends;

public interface IScalarTrend<T> : ITrend where T : struct, IComparable<T>
{
    TrendWindow<T> Window { get; }
    T? Latest { get; }
}
