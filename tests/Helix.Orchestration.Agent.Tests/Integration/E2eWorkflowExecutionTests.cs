using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Helix.Orchestration.Agent;
using Helix.Orchestration.Agent.Security;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Infrastructure.Persistence;
using Helix.Orchestration.Infrastructure.Tests.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Integration;

/// <summary>
/// Exercises the Agent's production composition through its public experiment and
/// evidence APIs.  This deliberately does not construct a runspace or dispatcher
/// directly: command authority, persistence and simulator effects must be wired by
/// the same registrations used by the Agent host.
/// </summary>
[Collection(nameof(AgentPostgresCollection))]
public sealed class E2eWorkflowExecutionTests(PostgresFixture postgres)
{
    [Fact]
    public async Task SubmitSimulatorWorkflow_PersistsConfirmedCanonicalCommandEvidence()
    {
        await using var host = await AgentTestHost.CreateAsync(postgres);
        using var client = host.Client;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-session");

        var response = await client.PostAsJsonAsync("/experiments/", new
        {
            name = "Canonical simulator flow",
            workflow = new
            {
                documentId = Guid.NewGuid().ToString("N"),
                name = "Canonical simulator flow",
                schemaVersion = "1.0",
                nodes = new[]
                {
                    new
                    {
                        nodeId = "set-flow",
                        nodeType = "Action",
                        label = "Set simulator flow",
                        parameters = new Dictionary<string, object?>
                        {
                            ["capability"] = "SetFlow",
                            ["device"] = "simulator",
                            ["flowRate"] = 1.5
                        }
                    }
                },
                edges = Array.Empty<object>()
            },
            approvedPolicyRevision = "simulation-policy-v1",
            metadata = new Dictionary<string, object?> { ["source"] = "production-e2e" }
        });

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        using var submission = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = submission.RootElement;
        var experimentId = root.GetProperty("experimentId").GetString();
        var runspaceId = root.GetProperty("activeRunspaceId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(experimentId));
        Assert.True(Guid.TryParseExact(runspaceId, "N", out var executionId));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("definitionHash").GetString()));

        await EventuallyAsync(async () =>
        {
            var experiment = await client.GetAsync($"/experiments/{experimentId}");
            return experiment.StatusCode == HttpStatusCode.OK;
        });

        string evidenceJson = string.Empty;
        await EventuallyAsync(async () =>
        {
            var evidence = await client.GetAsync($"/commands/evidence?executionId={executionId:D}");
            if (evidence.StatusCode != HttpStatusCode.OK)
                return false;

            evidenceJson = await evidence.Content.ReadAsStringAsync();
            return evidenceJson.Contains("SetFlow", StringComparison.Ordinal);
        });

        using var evidenceDocument = JsonDocument.Parse(evidenceJson);
        var commandId = evidenceDocument.RootElement
            .GetProperty("records")[0]
            .GetProperty("intent")
            .GetProperty("commandId")
            .GetGuid();
        var ledger = host.Services.GetRequiredService<ICommandLedger>();
        CommandDispatchRecord? dispatch = null;
        await EventuallyAsync(async () =>
        {
            dispatch = await ledger.GetDispatchAsync(commandId);
            return dispatch?.State == CommandDispatchState.Confirmed;
        });

        Assert.NotNull(dispatch);
        Assert.Equal(CommandDispatchState.Confirmed, dispatch.State);
        Assert.Contains("simulator", evidenceJson, StringComparison.Ordinal);
        Assert.Contains("SetFlow", evidenceJson, StringComparison.Ordinal);
        Assert.Contains("simulation-policy-v1", evidenceJson, StringComparison.Ordinal);
        Assert.Contains("S-1-5-21-test", evidenceJson, StringComparison.Ordinal);
        Assert.Contains("flowRate", evidenceJson, StringComparison.Ordinal);
        Assert.Contains("1.5", evidenceJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitWithoutDesktopSession_IsDeniedBeforeAnySimulatorEffect()
    {
        await using var host = await AgentTestHost.CreateAsync(postgres);
        using var client = host.Client;

        var response = await client.PostAsJsonAsync("/experiments/", new
        {
            name = "Denied simulator flow",
            workflow = new { documentId = Guid.NewGuid().ToString("N"), name = "Denied simulator flow", nodes = Array.Empty<object>(), edges = Array.Empty<object>() },
            approvedPolicyRevision = "simulation-policy-v1",
            metadata = (object?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task EventuallyAsync(Func<Task<bool>> predicate)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        do
        {
            if (await predicate())
                return;
            await Task.Delay(100);
        }
        while (DateTime.UtcNow < deadline);

        Assert.Fail("Timed out waiting for canonical Agent evidence.");
    }

    private sealed class AgentTestHost(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;
        public IServiceProvider Services => app.Services;

        public static async Task<AgentTestHost> CreateAsync(PostgresFixture postgres)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Testing"
            });
            builder.WebHost.UseTestServer();
            AgentApplicationComposition.ConfigureServices(builder);
            builder.Services.RemoveAll<DbConnectionFactory>();
            builder.Services.AddSingleton(postgres.ConnectionFactory ?? throw new InvalidOperationException("PostgreSQL fixture is unavailable."));

            var app = builder.Build();
            await app.Services.GetRequiredService<Helix.Orchestration.Devices.Drivers.SimulatorDevice>().StartAsync();
            app.Use(next => async context =>
            {
                if (context.Request.Headers.Authorization == "Bearer test-session")
                {
                    context.Items[DesktopSessionAuthenticationHandler.ActorItemKey] =
                        new VerifiedDesktopActor("session-1", "S-1-5-21-test", "Scientist");
                }
                await next(context);
            });

            // Production endpoint mapping and every production service registration
            // remain intact; only the local OS-session middleware is replaced by the
            // explicit verified TestServer actor above.
            AgentApplicationComposition.ConfigurePipeline(app, includeDesktopSessionAuthentication: false);
            await app.StartAsync();
            return new AgentTestHost(app, app.GetTestClient());
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }
}

/// <summary>Agent-side collection sharing the infrastructure fixture's real migration contract.</summary>
[CollectionDefinition(nameof(AgentPostgresCollection))]
public sealed class AgentPostgresCollection : ICollectionFixture<PostgresFixture>;
