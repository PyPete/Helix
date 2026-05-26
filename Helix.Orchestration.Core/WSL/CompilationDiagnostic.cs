namespace Helix.Orchestration.Core.WSL;

/// <summary>
/// A single diagnostic message produced during WSL compilation.
/// </summary>
/// <param name="Severity">Diagnostic severity level.</param>
/// <param name="Message">Human-readable diagnostic message.</param>
/// <param name="NodeId">Optional node identifier where the diagnostic originates.</param>
/// <param name="Code">Optional diagnostic code for programmatic filtering.</param>
public sealed record CompilationDiagnostic(
    DiagnosticSeverity Severity,
    string Message,
    string? NodeId = null,
    string? Code = null);
