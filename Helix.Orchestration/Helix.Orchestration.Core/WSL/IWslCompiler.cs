using Helix.Orchestration.Core.Devices;

namespace Helix.Orchestration.Core.WSL;

/// <summary>
/// Compiles a declarative <see cref="WslDocument"/> into an executable <see cref="RunspaceDefinition"/>.
/// </summary>
public interface IWslCompiler
{
    /// <summary>
    /// Compiles the WSL document into a runspace definition, performing schema validation,
    /// graph validation, device binding, and safety envelope injection.
    /// </summary>
    /// <param name="document">The WSL document to compile.</param>
    /// <param name="deviceRegistry">Registry of logical devices for binding resolution.</param>
    /// <returns>A compilation result containing the definition or diagnostics.</returns>
    CompilationResult Compile(WslDocument document, DeviceRegistry deviceRegistry);
}
