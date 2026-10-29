using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Helix.Orchestration.Agent.Api;
using Helix.Orchestration.Agent.Security;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Transports.Null;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Api;

/// <summary>Integration tests for the authenticated canonical device dispatch boundary.</summary>
public sealed class AgentDeviceEndpointsTests
{
    private static async Task<(WebApplication App, HttpClient Client, RecordingDispatcher Dispatcher)> BuildAppAsync(
        CommandAdmissionState admissionState = CommandAdmissionState.Allowed,
        bool authenticate = true)
    {
        var dispatcher = new RecordingDispatcher(admissionState);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddSingleton<ICommandDispatcher>(dispatcher);
        builder.Services.AddSingleton(new SimulatorDevice(new NullDeviceTransport(), NullLogger<SimulatorDevice>.Instance));

        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            if (context.Request.Headers.Authorization != "Bearer test-session")
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            context.Items[DesktopSessionAuthenticationHandler.ActorItemKey] =
                new VerifiedDesktopActor("session-1", "S-1-5-21-test", "Scientist");
            await next(context);
        });
        app.MapDeviceEndpoints();
        await app.StartAsync();

        var client = app.GetTestClient();
        if (authenticate)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-session");
        return (app, client, dispatcher);
    }

    [Fact]
    public async Task Execute_AuthenticatedSimulatorCommand_ProposesThenDispatchesCanonicalIntent()
    {
        var (app, client, dispatcher) = await BuildAppAsync();
        await using (app)
        {
            var response = await client.PostAsJsonAsync("/devices/simulator/execute", new
            {
                Capability = "SetFlow",
                Parameters = new Dictionary<string, object> { ["flowRate"] = 1.0 }
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(dispatcher.Proposals);
            Assert.Single(dispatcher.Dispatched);
            var intent = Assert.Single(dispatcher.Proposals);
            Assert.Equal("simulator", intent.DeviceId);
            Assert.Equal("SetFlow", intent.Capability);
            Assert.Equal(intent.CommandId, Assert.Single(dispatcher.Dispatched));
        }
    }

    [Fact]
    public async Task Execute_DeniedAdmission_ProducesNoDispatch()
    {
        var (app, client, dispatcher) = await BuildAppAsync(CommandAdmissionState.Held);
        await using (app)
        {
            var response = await client.PostAsJsonAsync("/devices/simulator/execute", new
            {
                Capability = "SetFlow",
                Parameters = new Dictionary<string, object> { ["flowRate"] = 1.0 }
            });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Single(dispatcher.Proposals);
            Assert.Empty(dispatcher.Dispatched);
        }
    }

    [Fact]
    public async Task Execute_WithoutAuthenticatedSession_ProducesNoProposalOrEffect()
    {
        var (app, client, dispatcher) = await BuildAppAsync(authenticate: false);
        await using (app)
        {
            var response = await client.PostAsJsonAsync("/devices/simulator/execute", new
            {
                Capability = "SetFlow",
                Parameters = new Dictionary<string, object> { ["flowRate"] = 1.0 }
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Empty(dispatcher.Proposals);
            Assert.Empty(dispatcher.Dispatched);
        }
    }

    [Fact]
    public async Task Execute_NonSimulatorDevice_Returns404WithoutProposal()
    {
        var (app, client, dispatcher) = await BuildAppAsync();
        await using (app)
        {
            var response = await client.PostAsJsonAsync("/devices/nonexistent/execute", new
            {
                Capability = "SetFlow",
                Parameters = new Dictionary<string, object> { ["flowRate"] = 1.0 }
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Empty(dispatcher.Proposals);
            Assert.Empty(dispatcher.Dispatched);
        }
    }

    private sealed class RecordingDispatcher(CommandAdmissionState admissionState) : ICommandDispatcher
    {
        public List<CommandIntent> Proposals { get; } = [];
        public List<Guid> Dispatched { get; } = [];

        public Task<CommandEvidence> ProposeAsync(CommandIntent intent, CancellationToken cancellationToken = default)
        {
            Proposals.Add(intent);
            return Task.FromResult(new CommandEvidence(
                Guid.NewGuid(),
                intent.CommandId,
                Proposals.Count,
                admissionState,
                [],
                DateTimeOffset.UtcNow));
        }

        public Task<CommandEvidence> DispatchAsync(Guid commandId, CancellationToken cancellationToken = default)
        {
            Dispatched.Add(commandId);
            return Task.FromResult(new CommandEvidence(
                Guid.NewGuid(), commandId, Dispatched.Count, CommandAdmissionState.Allowed, [], DateTimeOffset.UtcNow));
        }
    }
}
