namespace Helix.Orchestration.Core.WSL;

/// <summary>
/// Thrown when a <see cref="WslDocument"/> fails structural validation during compilation.
/// </summary>
public sealed class WslValidationException : Exception
{
    /// <summary>Individual validation error messages.</summary>
    public IReadOnlyList<string> Errors { get; }

    public WslValidationException(IReadOnlyList<string> errors)
        : base("WSL document validation failed:\n" + string.Join("\n", errors))
    {
        Errors = errors;
    }
}
