namespace Helix.Orchestration.Core.Capabilities;

/// <summary>
/// Describes the data type of a capability parameter.
/// </summary>
public enum ParameterType
{
    Numeric,
    Boolean,
    Enum,
    String,
    Duration
}

/// <summary>
/// Classifies the functional purpose of a capability.
/// </summary>
public enum CapabilityCategory
{
    Control,
    Measurement,
    Status,
    Utility
}

/// <summary>
/// Describes a single parameter accepted by a capability verb, including its type constraints
/// and display hints for auto-generating UI widgets.
/// </summary>
/// <param name="Name">Machine-readable parameter name.</param>
/// <param name="DisplayName">Human-readable label shown in the UI.</param>
/// <param name="Type">The data type of this parameter.</param>
/// <param name="Unit">Engineering unit (e.g. celsius, rpm). Empty string if unitless.</param>
/// <param name="Min">Minimum allowed value for Numeric parameters. Null if unrestricted.</param>
/// <param name="Max">Maximum allowed value for Numeric parameters. Null if unrestricted.</param>
/// <param name="Step">Increment step for Numeric parameters. Null if unrestricted.</param>
/// <param name="Options">Allowed values for Enum parameters. Null for non-enum types.</param>
/// <param name="DefaultValue">Default value presented to the user. Null if no default.</param>
public record ParameterSchema(
    string Name,
    string DisplayName,
    ParameterType Type,
    string Unit,
    double? Min,
    double? Max,
    double? Step,
    string[]? Options,
    object? DefaultValue);

/// <summary>
/// Describes a capability verb that can be invoked on a device adapter.
/// Used to auto-generate manual control UI widgets and to document the execute endpoint.
/// </summary>
/// <param name="VerbName">Machine-readable capability name matching the CapabilityRegistry key.</param>
/// <param name="DisplayName">Human-readable label for the capability.</param>
/// <param name="Description">Short description of what the capability does.</param>
/// <param name="Category">Functional category of the capability.</param>
/// <param name="Parameters">Ordered list of parameter schemas accepted by this capability.</param>
/// <param name="IsDestructive">True if invoking this capability has irreversible side effects.</param>
/// <param name="RequiresActiveRunspace">True if this capability may only be invoked during an active runspace.</param>
public record CapabilityDescriptor(
    string VerbName,
    string DisplayName,
    string Description,
    CapabilityCategory Category,
    ParameterSchema[] Parameters,
    bool IsDestructive,
    bool RequiresActiveRunspace);
