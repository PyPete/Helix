namespace Helix.Orchestration.Core.WSL;

/// <summary>
/// Result of compiling a <see cref="WslDocument"/> into a <see cref="RunspaceDefinition"/>.
/// </summary>
public sealed record CompilationResult
{
    /// <summary>
    /// Indicates whether compilation succeeded without errors.
    /// </summary>
    public bool Success => !Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// The compiled runspace definition, available only when <see cref="Success"/> is true.
    /// </summary>
    public RunspaceDefinition? Definition { get; init; }

    /// <summary>
    /// All diagnostics produced during compilation (errors, warnings, info).
    /// </summary>
    public IReadOnlyList<CompilationDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>
    /// Creates a successful compilation result.
    /// </summary>
    public static CompilationResult Succeeded(RunspaceDefinition definition, IReadOnlyList<CompilationDiagnostic>? diagnostics = null)
        => new() { Definition = definition, Diagnostics = diagnostics ?? [] };

    /// <summary>
    /// Creates a failed compilation result.
    /// </summary>
    public static CompilationResult Failed(IReadOnlyList<CompilationDiagnostic> diagnostics)
        => new() { Diagnostics = diagnostics };
}
