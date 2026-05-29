namespace Helix.Orchestration.Core.WSL;

/// <summary>
/// Severity level for compilation diagnostics.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>Informational message; does not affect compilation outcome.</summary>
    Info,

    /// <summary>Non-fatal issue; compilation succeeds but behaviour may be unexpected.</summary>
    Warning,

    /// <summary>Fatal issue; compilation fails.</summary>
    Error
}
