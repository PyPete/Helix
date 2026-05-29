using Helix.Orchestration.Core.Devices;
using Helix.Orchestration.Core.Safety;

namespace Helix.Orchestration.Core.WSL;

/// <summary>
/// Contract for WSL document compilation.
/// </summary>
public interface IWslCompiler
{
    /// <summary>
    /// Compiles a WSL document into an executable <see cref="RunspaceDefinition"/>.
    /// </summary>
    /// <param name="document">The document to compile.</param>
    /// <param name="deviceRegistry">Registry of devices available for binding validation.</param>
    /// <param name="safetyEngine">
    /// Optional safety engine.  When provided, current violations are snapshotted
    /// (read-only) and injected as guard nodes before execution.
    /// </param>
    CompilationResult Compile(WslDocument document, DeviceRegistry deviceRegistry, ISafetyEngine? safetyEngine = null);
}
