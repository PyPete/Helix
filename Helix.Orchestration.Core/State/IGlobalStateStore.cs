namespace Helix.Orchestration.Core.State;

/// <summary>
/// Contract for campaign-scoped and global variable storage.
/// Variables stored here are shared across all workflows within a campaign or globally across all campaigns.
/// </summary>
public interface IGlobalStateStore
{
    /// <summary>
    /// Sets a typed variable in the specified scope with JSON serialization for complex types.
    /// </summary>
    /// <param name="scope">The scope identifier (e.g., campaign ID or "global").</param>
    /// <param name="key">The variable key.</param>
    /// <param name="value">The variable value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetVariableAsync<T>(
        string scope,
        string key,
        T value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a typed variable from the specified scope with JSON deserialization for complex types.
    /// Returns default(T) if the variable does not exist or type conversion fails.
    /// </summary>
    /// <param name="scope">The scope identifier (e.g., campaign ID or "global").</param>
    /// <param name="key">The variable key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<T?> GetVariableAsync<T>(
        string scope,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a typed variable with a fallback default value if not found.
    /// </summary>
    /// <param name="scope">The scope identifier (e.g., campaign ID or "global").</param>
    /// <param name="key">The variable key.</param>
    /// <param name="defaultValue">The default value to return if the variable is not found.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<T> GetVariableOrDefaultAsync<T>(
        string scope,
        string key,
        T defaultValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a variable exists in the specified scope.
    /// </summary>
    /// <param name="scope">The scope identifier (e.g., campaign ID or "global").</param>
    /// <param name="key">The variable key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> VariableExistsAsync(
        string scope,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a variable from the specified scope.
    /// </summary>
    /// <param name="scope">The scope identifier (e.g., campaign ID or "global").</param>
    /// <param name="key">The variable key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteVariableAsync(
        string scope,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all variable keys in the specified scope.
    /// </summary>
    /// <param name="scope">The scope identifier (e.g., campaign ID or "global").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<string>> ListVariableKeysAsync(
        string scope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all variables in the specified scope as a read-only dictionary.
    /// </summary>
    /// <param name="scope">The scope identifier (e.g., campaign ID or "global").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyDictionary<string, object?>> GetAllVariablesAsync(
        string scope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all variables from the specified scope.
    /// </summary>
    /// <param name="scope">The scope identifier (e.g., campaign ID or "global").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearVariablesAsync(
        string scope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all scopes that have variables stored.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<string>> ListScopesAsync(CancellationToken cancellationToken = default);
}
