// Tests for POST /devices/{id}/execute in the Agent's DeviceEndpoints.
// Covers requirements C7 (standalone mode) and C9 (manual control path).
//
// Uses Microsoft.AspNetCore.TestHost with WebApplication so the actual
// DeviceEndpoints.MapDeviceEndpoints extension is exercised against a slim host
// that needs no external services (no Postgres, MQTT, or Cleipnir).

using System.Net;
using System.Net.Http.Json;
using Helix.Orchestration.Agent.Api;
using Helix.Orchestration.Core.Capabilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Api;

/// <summary>
/// Integration-style tests for POST /devices/{id}/execute.
/// A slim <see cref="WebApplication"/> is built with only <see cref="DeviceEndpoints"/>
/// registered so no external services are required.
/// </summary>
public sealed class AgentDeviceEndpointsTests
{
    // -------------------------------------------------------------------
    // Host factory
    // -------------------------------------------------------------------

    /// <summary>
    /// Builds a minimal <see cref="WebApplication"/> wired with
    /// <see cref="DeviceEndpoints.MapDeviceEndpoints"/> and the provided
    /// <see cref="CapabilityRegistry"/> configuration.
    /// </summary>
    private static async Task<(WebApplication App, HttpClient Client)> BuildAppAsync(
        Action<CapabilityRegistry>? registerCapabilities = null)
    {
        var registry = new CapabilityRegistry();
        registerCapabilities?.Invoke(registry);

        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();

        // Serialize enums as strings — consistent with the real Agent.
        builder.Services.ConfigureHttpJsonOptions(opts =>
            opts.SerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter()));

        builder.Services.AddSingleton(registry);
        // IRunspaceEventEmitter is an optional parameter in the endpoint handler.

        var app = builder.Build();

        app.MapDeviceEndpoints();

        await app.StartAsync();

        var client = app.GetTestClient();

        return (app, client);
    }

    // -------------------------------------------------------------------
    // Tests — C7 / C9
    // -------------------------------------------------------------------

    /// <summary>
    /// C7/C9: POST /devices/simulator/execute with a registered capability must return HTTP 200.
    /// </summary>
    [Fact]
    public async Task Execute_SimulatorSetFlow_Returns200WithSuccessTrue()
    {
        var (app, client) = await BuildAppAsync(reg =>
            reg.Register("simulator.SetFlow", new LambdaInvoker((_, _, _) =>
                Task.FromResult(CapabilityInvocationResult.Succeeded()))));

        await using (app)
        {
            var body = new { Capability = "SetFlow", Parameters = new Dictionary<string, object>() };
            var response = await client.PostAsJsonAsync("/devices/simulator/execute", body);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ExecuteResult>();
            Assert.NotNull(result);
            Assert.True(result!.success);
        }
    }

    /// <summary>
    /// C7/C9: POST /devices/nonexistent/execute must return HTTP 404.
    /// </summary>
    [Fact]
    public async Task Execute_NonexistentDevice_Returns404()
    {
        var (app, client) = await BuildAppAsync(reg =>
            reg.Register("simulator.SetFlow", new LambdaInvoker((_, _, _) =>
                Task.FromResult(CapabilityInvocationResult.Succeeded()))));

        await using (app)
        {
            var body = new { Capability = "SetFlow", Parameters = new Dictionary<string, object>() };
            var response = await client.PostAsJsonAsync("/devices/nonexistent/execute", body);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var json = await response.Content.ReadFromJsonAsync<ErrorResult>();
            Assert.NotNull(json);
            Assert.Contains("nonexistent", json!.Error, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// C7/C9: POST /devices/simulator/execute when CapabilityRegistry is empty must return HTTP 404.
    /// </summary>
    [Fact]
    public async Task Execute_EmptyRegistry_Returns404()
    {
        var (app, client) = await BuildAppAsync(); // no capabilities registered

        await using (app)
        {
            var body = new { Capability = "SetFlow", Parameters = new Dictionary<string, object>() };
            var response = await client.PostAsJsonAsync("/devices/simulator/execute", body);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    // -------------------------------------------------------------------
    // DTOs / helpers
    // -------------------------------------------------------------------

    private sealed record ExecuteResult(bool success, object? result, string? error);
    private sealed record ErrorResult(string Error);

    private sealed class LambdaInvoker : ICapabilityInvoker
    {
        private readonly Func<string, object?, CancellationToken, Task<CapabilityInvocationResult>> _fn;

        public LambdaInvoker(
            Func<string, object?, CancellationToken, Task<CapabilityInvocationResult>> fn)
            => _fn = fn;

        public Task<CapabilityInvocationResult> InvokeAsync(
            string capabilityName, object? parameters, CancellationToken cancellationToken)
            => _fn(capabilityName, parameters, cancellationToken);

        public Task<bool> IsAvailableAsync(string capabilityName)
            => Task.FromResult(true);
    }
}
