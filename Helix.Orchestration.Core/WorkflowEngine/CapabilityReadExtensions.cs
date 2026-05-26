using Helix.Orchestration.Core.Capabilities;

namespace Helix.Orchestration.Core.WorkflowEngine;

/// <summary>
/// Extension methods for reading device state as typed variables through capability invocation.
/// Enables workflows to retrieve device values (flow rates, temperatures, pressures, etc.)
/// without direct device coupling.
/// </summary>
public static class CapabilityReadExtensions
{
    /// <summary>
    /// Reads a typed value from a capability invocation.
    /// Capability name should be in the format "deviceId_capabilityName" (e.g., "vapourtec_status").
    /// Returns default(T) if the capability does not exist or the result cannot be converted.
    /// </summary>
    /// <param name="invoker">The capability invoker.</param>
    /// <param name="capabilityName">The capability name (e.g., "vapourtec_status").</param>
    /// <param name="propertyPath">Optional dot-separated property path for nested values (e.g., "FlowRate.Pump1").</param>
    /// <param name="parameters">Optional parameters for the capability invocation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <returns>The typed value or default(T) if not found or type conversion fails.</returns>
    public static async Task<T?> ReadDeviceValueAsync<T>(
        this ICapabilityInvoker invoker,
        string capabilityName,
        string? propertyPath = null,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(capabilityName))
            throw new ArgumentException("Capability name cannot be null or empty.", nameof(capabilityName));

        try
        {
            var invocationResult = await invoker.InvokeAsync(capabilityName, parameters, cancellationToken);

            if (!invocationResult.Success || invocationResult.ReturnValue == null)
                return default;

            var result = invocationResult.ReturnValue;

            // If a property path is specified, navigate into the result object
            if (!string.IsNullOrWhiteSpace(propertyPath))
            {
                result = NavigatePropertyPath(result, propertyPath);
                if (result == null)
                    return default;
            }

            // Direct type match
            if (result is T typedResult)
                return typedResult;

            // Attempt conversion for numeric types
            if (typeof(T).IsPrimitive || typeof(T) == typeof(decimal))
            {
                try
                {
                    return (T)Convert.ChangeType(result, typeof(T));
                }
                catch
                {
                    return default;
                }
            }

            return default;
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Reads a typed value with a fallback default if not found.
    /// </summary>
    public static async Task<T> ReadDeviceValueOrDefaultAsync<T>(
        this ICapabilityInvoker invoker,
        string capabilityName,
        T defaultValue,
        string? propertyPath = null,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var result = await invoker.ReadDeviceValueAsync<T>(
            capabilityName, propertyPath, parameters, cancellationToken);
        return result ?? defaultValue;
    }

    /// <summary>
    /// Navigates a dot-separated property path into a nested object.
    /// Example: "FlowRate.Pump1" will access result.FlowRate.Pump1
    /// </summary>
    private static object? NavigatePropertyPath(object source, string path)
    {
        var parts = path.Split('.');
        var current = source;

        foreach (var part in parts)
        {
            if (current == null)
                return null;

            var type = current.GetType();
            var property = type.GetProperty(part);

            if (property == null)
                return null;

            current = property.GetValue(current);
        }

        return current;
    }
}
