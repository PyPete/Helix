using System.Text.Json;

namespace Helix.Orchestration.Core.State;

/// <summary>
/// Extension methods for typed access to workflow variables with JSON serialization support.
/// Enables storage and retrieval of complex types (strings, numbers, arrays, dictionaries, nested objects).
/// </summary>
public static class WorkflowStateExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <summary>
    /// Sets a typed variable in workflow state with JSON serialization for complex types.
    /// </summary>
    public static async Task SetVariableAsync<T>(
        this IWorkflowStateStore store,
        string workflowId,
        string key,
        T value,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentException("Workflow ID cannot be null or empty.", nameof(workflowId));

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Variable key cannot be null or empty.", nameof(key));

        var state = await store.GetAsync(workflowId)
                    ?? new WorkflowState
                    {
                        WorkflowId = workflowId,
                        WorkflowName = "Unknown",
                        Status = "Running"
                    };

        // Serialize complex types to JSON for consistent storage
        state.Variables[key] = SerializeIfNeeded(value);

        await store.SaveAsync(state);
    }

    /// <summary>
    /// Gets a typed variable from workflow state with JSON deserialization for complex types.
    /// Returns default(T) if the variable does not exist or type conversion fails.
    /// </summary>
    public static async Task<T?> GetVariableAsync<T>(
        this IWorkflowStateStore store,
        string workflowId,
        string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentException("Workflow ID cannot be null or empty.", nameof(workflowId));

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Variable key cannot be null or empty.", nameof(key));

        var state = await store.GetAsync(workflowId);
        if (state?.Variables.TryGetValue(key, out var value) != true)
            return default;

        return DeserializeIfNeeded<T>(value);
    }

    /// <summary>
    /// Gets a typed variable with a fallback default value if not found.
    /// </summary>
    public static async Task<T> GetVariableOrDefaultAsync<T>(
        this IWorkflowStateStore store,
        string workflowId,
        string key,
        T defaultValue,
        CancellationToken cancellationToken = default)
    {
        var result = await store.GetVariableAsync<T>(workflowId, key, cancellationToken);
        return result ?? defaultValue;
    }

    /// <summary>
    /// Checks if a variable exists in workflow state.
    /// </summary>
    public static async Task<bool> VariableExistsAsync(
        this IWorkflowStateStore store,
        string workflowId,
        string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentException("Workflow ID cannot be null or empty.", nameof(workflowId));

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Variable key cannot be null or empty.", nameof(key));

        var state = await store.GetAsync(workflowId);
        return state?.Variables.ContainsKey(key) == true;
    }

    /// <summary>
    /// Deletes a variable from workflow state.
    /// </summary>
    public static async Task DeleteVariableAsync(
        this IWorkflowStateStore store,
        string workflowId,
        string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentException("Workflow ID cannot be null or empty.", nameof(workflowId));

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Variable key cannot be null or empty.", nameof(key));

        var state = await store.GetAsync(workflowId);
        if (state == null)
            return;

        state.Variables.Remove(key);
        await store.SaveAsync(state);
    }

    /// <summary>
    /// Lists all variable keys in workflow state.
    /// </summary>
    public static async Task<IReadOnlyList<string>> ListVariableKeysAsync(
        this IWorkflowStateStore store,
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentException("Workflow ID cannot be null or empty.", nameof(workflowId));

        var state = await store.GetAsync(workflowId);
        return state?.Variables.Keys.ToList() ?? [];
    }

    /// <summary>
    /// Gets all variables as a read-only dictionary.
    /// </summary>
    public static async Task<IReadOnlyDictionary<string, object?>> GetAllVariablesAsync(
        this IWorkflowStateStore store,
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentException("Workflow ID cannot be null or empty.", nameof(workflowId));

        var state = await store.GetAsync(workflowId);
        return state?.Variables ?? new Dictionary<string, object?>();
    }

    /// <summary>
    /// Clears all variables from workflow state.
    /// </summary>
    public static async Task ClearVariablesAsync(
        this IWorkflowStateStore store,
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentException("Workflow ID cannot be null or empty.", nameof(workflowId));

        var state = await store.GetAsync(workflowId);
        if (state == null)
            return;

        state.Variables.Clear();
        await store.SaveAsync(state);
    }

    // Serialization helpers

    private static object? SerializeIfNeeded<T>(T value)
    {
        if (value is null)
            return null;

        var type = typeof(T);

        // Primitives and strings can be stored directly
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return value;

        // Complex types: serialize to JSON string
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static T? DeserializeIfNeeded<T>(object? value)
    {
        if (value is null)
            return default;

        var targetType = typeof(T);

        // Direct assignment if types match
        if (value is T typedValue)
            return typedValue;

        // If stored as JSON string, deserialize
        if (value is string jsonString)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(jsonString, JsonOptions);
            }
            catch
            {
                return default;
            }
        }

        // Attempt direct conversion for numeric types
        if (targetType.IsPrimitive || targetType == typeof(decimal))
        {
            try
            {
                return (T)Convert.ChangeType(value, targetType);
            }
            catch
            {
                return default;
            }
        }

        return default;
    }
}
