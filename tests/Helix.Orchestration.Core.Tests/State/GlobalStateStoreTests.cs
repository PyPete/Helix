using Helix.Orchestration.Core.State;
using Xunit;

namespace Helix.Orchestration.Core.Tests.State;

public class GlobalStateStoreTests
{
    [Fact]
    public async Task SetVariableAsync_StoresSimpleValue_Successfully()
    {
        // Arrange
        var store = new InMemoryGlobalStateStore();
        var scope = "campaign-123";

        // Act
        await store.SetVariableAsync(scope, "sampleCount", 10);

        // Assert
        var value = await store.GetVariableAsync<int>(scope, "sampleCount");
        Assert.Equal(10, value);
    }

    [Fact]
    public async Task SetVariableAsync_StoresComplexObject_AsJson()
    {
        // Arrange
        var store = new InMemoryGlobalStateStore();
        var scope = "global";
        var complexObject = new { Experiment = "EXP001", Status = "Running", Parameters = new[] { 1, 2, 3 } };

        // Act
        await store.SetVariableAsync(scope, "experiment", complexObject);

        // Assert
        var value = await store.GetVariableAsync<dynamic>(scope, "experiment");
        Assert.NotNull(value);
    }

    [Fact]
    public async Task GetVariableAsync_ReturnsDefault_WhenVariableNotFound()
    {
        // Arrange
        var store = new InMemoryGlobalStateStore();
        var scope = "campaign-456";

        // Act
        var value = await store.GetVariableAsync<string>(scope, "missing");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public async Task GetVariableOrDefaultAsync_ReturnsFallback_WhenVariableNotFound()
    {
        // Arrange
        var store = new InMemoryGlobalStateStore();
        var scope = "campaign-789";

        // Act
        var value = await store.GetVariableOrDefaultAsync(scope, "missing", "fallback");

        // Assert
        Assert.Equal("fallback", value);
    }

    [Fact]
    public async Task VariableExistsAsync_ReturnsTrue_WhenVariableExists()
    {
        // Arrange
        var store = new InMemoryGlobalStateStore();
        var scope = "global";
        await store.SetVariableAsync(scope, "systemReady", true);

        // Act
        var exists = await store.VariableExistsAsync(scope, "systemReady");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task VariableExistsAsync_ReturnsFalse_WhenVariableDoesNotExist()
    {
        // Arrange
        var store = new InMemoryGlobalStateStore();
        var scope = "global";

        // Act
        var exists = await store.VariableExistsAsync(scope, "nonExistent");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteVariableAsync_RemovesVariable_Successfully()
    {
        // Arrange
        var store = new InMemoryGlobalStateStore();
        var scope = "campaign-101";
        await store.SetVariableAsync(scope, "toDelete", "value");

        // Act
        await store.DeleteVariableAsync(scope, "toDelete");

        // Assert
        var exists = await store.VariableExistsAsync(scope, "toDelete");
        Assert.False(exists);
    }

    [Fact]
    public async Task ListVariableKeysAsync_ReturnsAllKeys_InScope()
    {
        // Arrange
        var store = new InMemoryGlobalStateStore();
        var scope = "campaign-202";
        await store.SetVariableAsync(scope, "key1", "value1");
        await store.SetVariableAsync(scope, "key2", "value2");
        await store.SetVariableAsync(scope, "key3", "value3");

        // Act
        var keys = await store.ListVariableKeysAsync(scope);

        // Assert
        Assert.Equal(3, keys.Count);
        Assert.Contains("key1", keys);
        Assert.Contains("key2", keys);
        Assert.Contains("key3", keys);
    }

    [Fact]
    public async Task GetAllVariablesAsync_ReturnsAllVariables_InScope()
    {
        // Arrange
        var store = new InMemoryGlobalStateStore();
        var scope = "campaign-303";
        await store.SetVariableAsync(scope, "key1", "value1");
        await store.SetVariableAsync(scope, "key2", 99);

        // Act
        var variables = await store.GetAllVariablesAsync(scope);

        // Assert
        Assert.Equal(2, variables.Count);
        Assert.True(variables.ContainsKey("key1"));
        Assert.True(variables.ContainsKey("key2"));
    }

    [Fact]
    public async Task ClearVariablesAsync_RemovesAllVariables_InScope()
    {
        // Arrange
        var store = new InMemoryGlobalStateStore();
        var scope = "campaign-404";
        await store.SetVariableAsync(scope, "key1", "value1");
        await store.SetVariableAsync(scope, "key2", "value2");

        // Act
        await store.ClearVariablesAsync(scope);

        // Assert
        var keys = await store.ListVariableKeysAsync(scope);
        Assert.Empty(keys);
    }

    [Fact]
    public async Task ListScopesAsync_ReturnsAllScopes_WithVariables()
    {
        // Arrange
        var store = new InMemoryGlobalStateStore();
        await store.SetVariableAsync("global", "key1", "value1");
        await store.SetVariableAsync("campaign-123", "key2", "value2");
        await store.SetVariableAsync("campaign-456", "key3", "value3");

        // Act
        var scopes = await store.ListScopesAsync();

        // Assert
        Assert.Equal(3, scopes.Count);
        Assert.Contains("global", scopes);
        Assert.Contains("campaign-123", scopes);
        Assert.Contains("campaign-456", scopes);
    }

    [Fact]
    public async Task VariableIsolation_BetweenScopes_IsRespected()
    {
        // Arrange
        var store = new InMemoryGlobalStateStore();
        await store.SetVariableAsync("campaign-A", "temperature", 20.0);
        await store.SetVariableAsync("campaign-B", "temperature", 30.0);

        // Act
        var tempA = await store.GetVariableAsync<double>("campaign-A", "temperature");
        var tempB = await store.GetVariableAsync<double>("campaign-B", "temperature");

        // Assert
        Assert.Equal(20.0, tempA);
        Assert.Equal(30.0, tempB);
    }
}
