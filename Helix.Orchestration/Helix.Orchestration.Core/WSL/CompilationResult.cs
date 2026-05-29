namespace Helix.Orchestration.Core.WSL;

/// <summary>
/// Outcome of a WSL compilation attempt.
/// </summary>
public sealed class CompilationResult
{
    /// <summary>Whether compilation succeeded (no error-severity diagnostics).</summary>
    public bool Success { get; }

    /// <summary>
    /// The produced runspace definition.
    /// <c>null</c> when <see cref="Success"/> is <c>false</c>.
    /// </summary>
    public RunspaceDefinition? Definition { get; }

    /// <summary>All diagnostics (info, warning, and error) produced during compilation.</summary>
    public IReadOnlyList<CompilationDiagnostic> Diagnostics { get; }

    private CompilationResult(bool success, RunspaceDefinition? definition, IReadOnlyList<CompilationDiagnostic> diagnostics)
    {
        Success = success;
        Definition = definition;
        Diagnostics = diagnostics;
    }

    /// <summary>Creates a successful compilation result.</summary>
    public static CompilationResult Succeeded(RunspaceDefinition definition, IEnumerable<CompilationDiagnostic> diagnostics)
        => new(true, definition, diagnostics.ToList());

    /// <summary>Creates a failed compilation result.</summary>
    public static CompilationResult Failed(IEnumerable<CompilationDiagnostic> diagnostics)
        => new(false, null, diagnostics.ToList());
}
