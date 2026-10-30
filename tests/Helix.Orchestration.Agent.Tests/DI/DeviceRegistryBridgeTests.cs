// Tests for the CapabilityRegistry → DeviceRegistry bridge logic.
// Covers requirements C4 (experiment execution — DeviceAction nodes) and
// C8 (device registry view — manual control bridge).
//
// These are pure unit tests: CapabilityRegistry and DeviceRegistry are instantiated
// directly without a web host, and the bridge lambda from Program.cs is replicated inline.
// They verify the three observable outcomes produced by the factory registered at Program.cs line 224.

using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Core.Devices;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.DI;

/// <summary>
/// Unit tests for the <c>AddSingleton&lt;DeviceRegistry&gt;</c> factory lambda in
/// <c>Program.cs</c> that bridges <see cref="CapabilityRegistry"/> →
/// <see cref="DeviceRegistry"/> at startup.
/// </summary>
public sealed class DeviceRegistryBridgeTests
{
    // -------------------------------------------------------------------
    // Bridge helper (mirrors the factory lambda in Program.cs exactly)
    // -------------------------------------------------------------------

    /// <summary>
    /// Replicates the factory lambda from Program.cs so the same logic
    /// can be exercised without spinning up a web host.
    /// </summary>
    private static DeviceRegistry BuildDeviceRegistry(CapabilityRegistry capRegistry)
    {
        var deviceRegistry = new DeviceRegistry();

        var prefixes = capRegistry.GetAllCapabilityNames()
            .Select(name => { var dot = name.IndexOf('.'); return dot > 0 ? name[..dot] : name; })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var prefix in prefixes)
        {
            var caps = capRegistry.GetAllCapabilityNames()
                .Where(n => { var dot = n.IndexOf('.'); return string.Equals(dot > 0 ? n[..dot] : n, prefix, StringComparison.OrdinalIgnoreCase); })
                .Select(n => { var dot = n.IndexOf('.'); return dot > 0 ? n[(dot + 1)..] : n; })
                .ToList();

            deviceRegistry.Register(new DeviceDescriptor(
                prefix, prefix, "device", caps,
                new Dictionary<string, object?>()));
        }

        return deviceRegistry;
    }

    // -------------------------------------------------------------------
    // Tests — C4 / C8
    // -------------------------------------------------------------------

    /// <summary>
    /// C4/C8 Test 1: A CapabilityRegistry with "simulator.SetFlow" and "simulator.GetFlow"
    /// must produce a DeviceRegistry where TryGetDevice("simulator") returns true.
    /// </summary>
    [Fact]
    public void Bridge_SimulatorCapabilities_SimulatorDeviceIsRegistered()
    {
        var capRegistry = new CapabilityRegistry();
        capRegistry.Register("simulator.SetFlow",  new NoOpInvoker());
        capRegistry.Register("simulator.GetFlow",  new NoOpInvoker());

        var deviceRegistry = BuildDeviceRegistry(capRegistry);

        Assert.True(deviceRegistry.TryGetDevice("simulator", out var descriptor));
        Assert.NotNull(descriptor);
        Assert.Equal("simulator", descriptor!.DeviceAlias);
    }

    /// <summary>
    /// C4/C8 Test 2: A CapabilityRegistry with capabilities for two adapters must
    /// produce exactly two devices.
    /// </summary>
    [Fact]
    public void Bridge_MultipleAdapters_CorrectDeviceCountAndAliases()
    {
        var capRegistry = new CapabilityRegistry();
        capRegistry.Register("simulator.SetFlow",  new NoOpInvoker());
        capRegistry.Register("pump.Start",         new NoOpInvoker());
        capRegistry.Register("pump.Stop",          new NoOpInvoker());

        var deviceRegistry = BuildDeviceRegistry(capRegistry);

        var devices = deviceRegistry.GetAllDevices();
        Assert.Equal(2, devices.Count);
        Assert.True(deviceRegistry.TryGetDevice("simulator", out _));
        Assert.True(deviceRegistry.TryGetDevice("pump",      out _));
    }

    /// <summary>
    /// C4/C8 Test 3: An empty CapabilityRegistry must produce an empty DeviceRegistry.
    /// </summary>
    [Fact]
    public void Bridge_EmptyCapabilityRegistry_EmptyDeviceRegistry()
    {
        var capRegistry    = new CapabilityRegistry();
        var deviceRegistry = BuildDeviceRegistry(capRegistry);

        Assert.Empty(deviceRegistry.GetAllDevices());
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------

    private sealed class NoOpInvoker : ICapabilityInvoker
    {
        public Task<CapabilityInvocationResult> InvokeAsync(
            string capabilityName, object? parameters, CancellationToken cancellationToken)
            => Task.FromResult(CapabilityInvocationResult.Succeeded());

        public Task<bool> IsAvailableAsync(string capabilityName)
            => Task.FromResult(true);
    }
}
