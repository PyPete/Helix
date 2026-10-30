namespace Helix.Orchestration.Devices.Abstractions;

/// <summary>
/// Describes a device capability that can be invoked through the ICapabilityInvoker abstraction.
/// </summary>
public sealed class CapabilityDescriptor
{
    /// <summary>
    /// The unique name of the capability (e.g., "SetTemperature", "ReadPressure").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description of what the capability does.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Optional JSON schema describing the expected parameter structure.
    /// </summary>
    public string? ParameterSchema { get; init; }

    /// <summary>
    /// Optional JSON schema describing the return value structure.
    /// </summary>
    public string? ReturnSchema { get; init; }

    /// <summary>
    /// Creates a simple capability descriptor without schemas.
    /// </summary>
    public static CapabilityDescriptor Create(string name, string description) =>
        new() { Name = name, Description = description };

    /// <summary>
    /// Creates a capability descriptor with parameter and return schemas.
    /// </summary>
    public static CapabilityDescriptor CreateWithSchemas(
        string name,
        string description,
        string? parameterSchema = null,
        string? returnSchema = null) =>
        new()
        {
            Name = name,
            Description = description,
            ParameterSchema = parameterSchema,
            ReturnSchema = returnSchema
        };
}
