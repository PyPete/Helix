namespace Helix.Orchestration.Core.WSL;

/// <summary>
/// A single diagnostic message produced during WSL compilation.
/// </summary>
/// <param name="Severity">How severe the issue is.</param>
/// <param name="Message">Human-readable description of the issue.</param>
/// <param name="NodeId">Optional node ID the diagnostic is associated with.</param>
/// <param name="Code">Stable diagnostic code (e.g. "WSL001").</param>
public sealed record CompilationDiagnostic(
    DiagnosticSeverity Severity,
    string Message,
    string? NodeId = null,
    string? Code = null);
