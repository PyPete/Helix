using Helix.Orchestration.Core.Capabilities;
using Xunit;

namespace Helix.Orchestration.Core.Tests.Capabilities;

public class CapabilityRegistryTests
{
    [Fact]
    public void Register_AddsCapability_Successfully()
    {
        // Arrange
        var registry = new CapabilityRegistry();
        var invoker = new TestCapabilityInvoker("TestValue");

        // Act
        registry.Register("TestCapability", invoker);

        // Assert
        var names = registry.GetAllCapabilityNames().ToList();
        Assert.Contains("TestCapability", names);
    }

    [Fact]
    public void Register_ThrowsException_WhenCapabilityAlreadyRegistered()
    {
        // Arrange
        var registry = new CapabilityRegistry();
        var invoker = new TestCapabilityInvoker("TestValue");
        registry.Register("TestCapability", invoker);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => registry.Register("TestCapability", invoker));
    }

    [Fact]
    public async Task InvokeAsync_CallsRegisteredCapability_ReturnsResult()
    {
        // Arrange
        var registry = new CapabilityRegistry();
        var invoker = new TestCapabilityInvoker("ExpectedValue");
        registry.Register("TestCapability", invoker);

        // Act
        var result = await registry.InvokeAsync("TestCapability", new { Parameter = "test" });

        // Assert
        Assert.True(result.Success);
        Assert.Equal("ExpectedValue", result.ReturnValue);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsFailedResult_WhenCapabilityNotRegistered()
    {
        // Arrange
        var registry = new CapabilityRegistry();

        // Act
        var result = await registry.InvokeAsync("NonExistentCapability");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not registered", result.ErrorMessage);
    }

    [Fact]
    public async Task IsAvailableAsync_ReturnsTrue_WhenCapabilityRegisteredAndAvailable()
    {
        // Arrange
        var registry = new CapabilityRegistry();
        var invoker = new TestCapabilityInvoker("TestValue", isAvailable: true);
        registry.Register("TestCapability", invoker);

        // Act
        var isAvailable = await registry.IsAvailableAsync("TestCapability");

        // Assert
        Assert.True(isAvailable);
    }

    [Fact]
    public async Task IsAvailableAsync_ReturnsFalse_WhenCapabilityNotRegistered()
    {
        // Arrange
        var registry = new CapabilityRegistry();

        // Act
        var isAvailable = await registry.IsAvailableAsync("NonExistentCapability");

        // Assert
        Assert.False(isAvailable);
    }

    [Fact]
    public void Unregister_RemovesCapability_Successfully()
    {
        // Arrange
        var registry = new CapabilityRegistry();
        var invoker = new TestCapabilityInvoker("TestValue");
        registry.Register("TestCapability", invoker);

        // Act
        registry.Unregister("TestCapability");

        // Assert
        var names = registry.GetAllCapabilityNames().ToList();
        Assert.DoesNotContain("TestCapability", names);
    }

    // Test capability invoker that returns a fixed value
    private class TestCapabilityInvoker : ICapabilityInvoker
    {
        private readonly object _returnValue;
        private readonly bool _isAvailable;

        public TestCapabilityInvoker(object returnValue, bool isAvailable = true)
        {
            _returnValue = returnValue;
            _isAvailable = isAvailable;
        }

        public Task<CapabilityInvocationResult> InvokeAsync(
            string capabilityName,
            object? parameters = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CapabilityInvocationResult.Succeeded(_returnValue));
        }

        public Task<bool> IsAvailableAsync(string capabilityName)
        {
            return Task.FromResult(_isAvailable);
        }
    }
}
