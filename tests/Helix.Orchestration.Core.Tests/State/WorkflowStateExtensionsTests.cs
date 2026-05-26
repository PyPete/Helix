using Helix.Orchestration.Core.State;
using Xunit;

namespace Helix.Orchestration.Core.Tests.State;

public class WorkflowStateExtensionsTests
{
    [Fact]
    public async Task SetVariableAsync_StoresSimpleValue_Successfully()
    {
        // Arrange
        var store = new WorkflowStateStore();
        var workflowId = "test-workflow-1";

        // Act
        await store.SetVariableAsync(workflowId, "temperature", 25.5);

        // Assert
        var value = await store.GetVariableAsync<double>(workflowId, "temperature");
        Assert.Equal(25.5, value);
    }

    [Fact]
    public async Task SetVariableAsync_StoresComplexObject_AsJson()
    {
        // Arrange
        var store = new WorkflowStateStore();
        var workflowId = "test-workflow-2";
        var complexObject = new { Name = "Sample1", Count = 42, Tags = new[] { "tag1", "tag2" } };

        // Act
        await store.SetVariableAsync(workflowId, "sample", complexObject);

        // Assert
        var value = await store.GetVariableAsync<dynamic>(workflowId, "sample");
        Assert.NotNull(value);
    }

    [Fact]
    public async Task GetVariableAsync_ReturnsDefault_WhenVariableNotFound()
    {
        // Arrange
        var store = new WorkflowStateStore();
        var workflowId = "test-workflow-3";

        // Act
        var value = await store.GetVariableAsync<string>(workflowId, "missing");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public async Task GetVariableOrDefaultAsync_ReturnsFallback_WhenVariableNotFound()
    {
        // Arrange
        var store = new WorkflowStateStore();
        var workflowId = "test-workflow-4";

        // Act
        var value = await store.GetVariableOrDefaultAsync(workflowId, "missing", "default-value");

        // Assert
        Assert.Equal("default-value", value);
    }

    [Fact]
    public async Task VariableExistsAsync_ReturnsTrue_WhenVariableExists()
    {
        // Arrange
        var store = new WorkflowStateStore();
        var workflowId = "test-workflow-5";
        await store.SetVariableAsync(workflowId, "exists", true);

        // Act
        var exists = await store.VariableExistsAsync(workflowId, "exists");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task VariableExistsAsync_ReturnsFalse_WhenVariableDoesNotExist()
    {
        // Arrange
        var store = new WorkflowStateStore();
        var workflowId = "test-workflow-6";

        // Act
        var exists = await store.VariableExistsAsync(workflowId, "missing");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteVariableAsync_RemovesVariable_Successfully()
    {
        // Arrange
        var store = new WorkflowStateStore();
        var workflowId = "test-workflow-7";
        await store.SetVariableAsync(workflowId, "toDelete", "value");

        // Act
        await store.DeleteVariableAsync(workflowId, "toDelete");

        // Assert
        var exists = await store.VariableExistsAsync(workflowId, "toDelete");
        Assert.False(exists);
    }

    [Fact]
    public async Task ListVariableKeysAsync_ReturnsAllKeys_Successfully()
    {
        // Arrange
        var store = new WorkflowStateStore();
        var workflowId = "test-workflow-8";
        await store.SetVariableAsync(workflowId, "key1", "value1");
        await store.SetVariableAsync(workflowId, "key2", "value2");
        await store.SetVariableAsync(workflowId, "key3", "value3");

        // Act
        var keys = await store.ListVariableKeysAsync(workflowId);

        // Assert
        Assert.Equal(3, keys.Count);
        Assert.Contains("key1", keys);
        Assert.Contains("key2", keys);
        Assert.Contains("key3", keys);
    }

    [Fact]
    public async Task GetAllVariablesAsync_ReturnsAllVariables_Successfully()
    {
        // Arrange
        var store = new WorkflowStateStore();
        var workflowId = "test-workflow-9";
        await store.SetVariableAsync(workflowId, "key1", "value1");
        await store.SetVariableAsync(workflowId, "key2", 42);

        // Act
        var variables = await store.GetAllVariablesAsync(workflowId);

        // Assert
        Assert.Equal(2, variables.Count);
        Assert.True(variables.ContainsKey("key1"));
        Assert.True(variables.ContainsKey("key2"));
    }

    [Fact]
    public async Task ClearVariablesAsync_RemovesAllVariables_Successfully()
    {
        // Arrange
        var store = new WorkflowStateStore();
        var workflowId = "test-workflow-10";
        await store.SetVariableAsync(workflowId, "key1", "value1");
        await store.SetVariableAsync(workflowId, "key2", "value2");

        // Act
        await store.ClearVariablesAsync(workflowId);

        // Assert
        var keys = await store.ListVariableKeysAsync(workflowId);
        Assert.Empty(keys);
    }
}
