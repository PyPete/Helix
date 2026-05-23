namespace Helix.Orchestration.Core.Leasing;

/// <summary>
/// Controls how a device may be shared between concurrent workflows.
/// </summary>
public enum LeaseMode
{
    /// <summary>
    /// Only one workflow may hold a lease at a time.
    /// </summary>
    Exclusive,

    /// <summary>
    /// Multiple workflows may hold a read-only lease simultaneously.
    /// </summary>
    Shared
}
