namespace Helix.Orchestration.Core.WSL;

/// <summary>
/// Diagnostic message severity level.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>Informational message.</summary>
    Info,

    /// <summary>Warning — compilation succeeds but may indicate a problem.</summary>
    Warning,

    /// <summary>Error — compilation cannot proceed.</summary>
    Error
}
