namespace Helix.Orchestration.Devices.Transports.CDll;

/// <summary>
/// Configuration options for C DLL (native library) transport via P/Invoke.
/// </summary>
public sealed class CDllTransportOptions
{
    /// <summary>
    /// Full path to the DLL directory (e.g., "C:\libs\native").
    /// </summary>
    public string? DllPath { get; init; }

    /// <summary>
    /// DLL filename (e.g., "usbtc08.dll", "portavo.dll").
    /// </summary>
    public required string DllName { get; init; }

    /// <summary>
    /// Whether to use architecture-specific subdirectories (e.g., "runtimes\win-x64\native\").
    /// Mirrors PopNextGen's pattern for architecture-aware native library loading.
    /// </summary>
    public bool UseArchitectureSubfolder { get; init; } = false;

    /// <summary>
    /// Maps logical function names to exported DLL function names.
    /// Example: { "Init" => "PortavoInitDll", "Connect" => "PortavoConnect" }
    /// </summary>
    public Dictionary<string, string> FunctionMap { get; init; } = new();

    /// <summary>
    /// Optional handle validation function name.
    /// Should return a boolean or int indicating whether a handle is valid.
    /// </summary>
    public string? HandleValidationFunction { get; init; }

    /// <summary>
    /// Maximum handle value before considering it invalid (default: 0 = disabled).
    /// Used for validating device handles from open/init calls.
    /// </summary>
    public int MaxValidHandle { get; init; } = 0;
}
