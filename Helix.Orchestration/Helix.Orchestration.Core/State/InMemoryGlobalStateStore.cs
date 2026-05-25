using System.Collections.Concurrent;
using System.Text.Json;

namespace Helix.Orchestration.Core.State;

/// <summary>
/// In-memory implementation of global state storage with scoped variable management.
/// Supports campaign-scoped and global variables with JSON serialization for complex types.
/// Thread-safe for concurrent workflow access.
/// </summary>
public class InMemoryGlobalStateStore : IGlobalStateStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object?>> _scopes = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <inheritdoc />
    public Task SetVariableAsync<T>(
        string scope,
        string key,
        T value,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("Scope cannot be null or empty.", nameof(scope));

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Variable key cannot be null or empty.", nameof(key));

        var scopeVars = _scopes.GetOrAdd(scope, _ => new ConcurrentDictionary<string, object?>());
        scopeVars[key] = SerializeIfNeeded(value);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<T?> GetVariableAsync<T>(
        string scope,
        string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("Scope cannot be null or empty.", nameof(scope));

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Variable key cannot be null or empty.", nameof(key));

        if (!_scopes.TryGetValue(scope, out var scopeVars) ||
            !scopeVars.TryGetValue(key, out var value))
        {
            return Task.FromResult(default(T));
        }

        return Task.FromResult(DeserializeIfNeeded<T>(value));
    }

    /// <inheritdoc />
    public async Task<T> GetVariableOrDefaultAsync<T>(
        string scope,
        string key,
        T defaultValue,
        CancellationToken cancellationToken = default)
    {
        var result = await GetVariableAsync<T>(scope, key, cancellationToken);
        return result ?? defaultValue;
    }

    /// <inheritdoc />
    public Task<bool> VariableExistsAsync(
        string scope,
        string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("Scope cannot be null or empty.", nameof(scope));

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Variable key cannot be null or empty.", nameof(key));

        var exists = _scopes.TryGetValue(scope, out var scopeVars) && scopeVars.ContainsKey(key);
        return Task.FromResult(exists);
    }

    /// <inheritdoc />
    public Task DeleteVariableAsync(
        string scope,
        string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("Scope cannot be null or empty.", nameof(scope));

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Variable key cannot be null or empty.", nameof(key));

        if (_scopes.TryGetValue(scope, out var scopeVars))
        {
            scopeVars.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListVariableKeysAsync(
        string scope,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("Scope cannot be null or empty.", nameof(scope));

        if (!_scopes.TryGetValue(scope, out var scopeVars))
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        return Task.FromResult<IReadOnlyList<string>>(scopeVars.Keys.ToList());
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, object?>> GetAllVariablesAsync(
        string scope,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("Scope cannot be null or empty.", nameof(scope));

        if (!_scopes.TryGetValue(scope, out var scopeVars))
        {
            return Task.FromResult<IReadOnlyDictionary<string, object?>>(
                new Dictionary<string, object?>());
        }

        var snapshot = new Dictionary<string, object?>(scopeVars);
        return Task.FromResult<IReadOnlyDictionary<string, object?>>(snapshot);
    }

    /// <inheritdoc />
    public Task ClearVariablesAsync(
        string scope,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("Scope cannot be null or empty.", nameof(scope));

        if (_scopes.TryGetValue(scope, out var scopeVars))
        {
            scopeVars.Clear();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListScopesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<string>>(_scopes.Keys.ToList());
    }

    // Serialization helpers (duplicated from WorkflowStateExtensions for now)

    private static object? SerializeIfNeeded<T>(T value)
    {
        if (value is null)
            return null;

        var type = typeof(T);

        // Primitives and strings can be stored directly
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
            type == typeof(DateTime) || type == typeof(DateTimeOffset))
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
