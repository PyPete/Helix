using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Helix.Orchestration.Agent.Api;
using Helix.Orchestration.Agent.Security;
using Helix.Orchestration.Core.ScientificSetup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Api;

public sealed class ScientificSetupEndpointsTests
{
    [Fact]
    public async Task Scientific_routes_use_authenticated_actor_and_return_desktop_contracts()
    {
        var repository = new ScientificSetupRepository();
        var (app, client) = await BuildAppAsync(repository, authenticate: true);
        await using (app)
        {
            var operationId = Guid.NewGuid();
            var created = await client.PostAsJsonAsync("/api/scientific-setup/apparatus/", new { operationId, name = "Reactor", description = "Draft" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var receipt = await created.Content.ReadFromJsonAsync<ScientificOperationResponse>();
            Assert.NotNull(receipt);
            Assert.Equal(operationId, receipt.OperationId);
            Assert.Equal("completed", receipt.Status);

            var list = await client.GetFromJsonAsync<List<ScientificApparatusResponse>>("/api/scientific-setup/apparatus/");
            var apparatus = Assert.Single(list!);
            Assert.Equal(receipt.ApparatusId, apparatus.ApparatusId);
            Assert.Null(apparatus.RevisionId);

            var published = await client.PostAsJsonAsync($"/api/scientific-setup/apparatus/{apparatus.ApparatusId}/publications", new { operationId = Guid.NewGuid(), expectedRevisionId = (Guid?)null, note = "Ready" });
            Assert.Equal(HttpStatusCode.OK, published.StatusCode);
            var publication = await published.Content.ReadFromJsonAsync<ScientificOperationResponse>();
            Assert.NotNull(publication?.RevisionId);

            var history = await client.GetFromJsonAsync<List<ScientificOperationResponse>>($"/api/scientific-setup/apparatus/{apparatus.ApparatusId}/publications");
            Assert.Contains(history!, value => value.OperationId == publication.OperationId && value.RevisionId == publication.RevisionId);
            var revision = await client.GetFromJsonAsync<ScientificApparatusResponse>($"/api/scientific-setup/apparatus/{apparatus.ApparatusId}/publications/{publication!.RevisionId}");
            Assert.Equal(publication.RevisionId, revision!.RevisionId);
            var operation = await client.GetFromJsonAsync<ScientificOperationResponse>($"/api/scientific-setup/operations/{publication.OperationId}");
            Assert.Equal(publication.RevisionId, operation!.RevisionId);
        }
    }

    [Fact]
    public async Task Scientific_routes_reject_unauthenticated_requests()
    {
        var (app, client) = await BuildAppAsync(new ScientificSetupRepository(), authenticate: false);
        await using (app)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/scientific-setup/apparatus/")).StatusCode);
    }

    private static async Task<(WebApplication App, HttpClient Client)> BuildAppAsync(IScientificSetupRepository repository, bool authenticate)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(repository);
        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            if (context.Request.Headers.Authorization != "Bearer test-session")
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            context.Items[DesktopSessionAuthenticationHandler.ActorItemKey] = new VerifiedDesktopActor("session-1", "test-actor", "Scientist");
            await next(context);
        });
        app.MapScientificSetupEndpoints();
        await app.StartAsync();
        var client = app.GetTestClient();
        if (authenticate) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-session");
        return (app, client);
    }

    private sealed class ScientificSetupRepository : IScientificSetupRepository
    {
        private readonly Dictionary<Guid, ScientificApparatusSummary> _apparatus = [];
        private readonly Dictionary<Guid, ScientificRevision> _revisions = [];
        private readonly Dictionary<Guid, ScientificOperationReceipt> _operations = [];

        public Task<(ApparatusDefinition Apparatus, long Token, ScientificOperationReceipt Receipt)> CreateAsync(ApparatusDefinition apparatus, string actorSid, Guid operationId, CancellationToken ct = default)
        {
            _apparatus.Add(apparatus.ApparatusId, new(apparatus.ApparatusId, apparatus.Name, null));
            var receipt = new ScientificOperationReceipt(operationId, "apparatus.create", apparatus.ApparatusId, actorSid, DateTimeOffset.UtcNow, "{}");
            _operations.Add(operationId, receipt);
            return Task.FromResult((apparatus, 1L, receipt));
        }

        public Task<(ApparatusDefinition Apparatus, long Token, ScientificOperationReceipt Receipt)> SaveDraftAsync(ApparatusDefinition apparatus, long expectedToken, string actorSid, Guid operationId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<(ApparatusDefinition Apparatus, long Token)> ReadDraftAsync(Guid apparatusId, string actorSid, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<(ApparatusDefinition Apparatus, long Token, ScientificOperationReceipt Receipt)> AdoptRevisionAsync(Guid apparatusId, Guid revisionId, long expectedToken, string actorSid, Guid operationId, CancellationToken ct = default) => throw new NotImplementedException();

        public Task<(ScientificRevision Revision, ScientificOperationReceipt Receipt)> PublishAsync(Guid apparatusId, Guid? expectedRevisionId, string name, string actorSid, Guid operationId, CancellationToken ct = default)
        {
            var revision = new ScientificRevision(Guid.NewGuid(), apparatusId, name, "hash", actorSid, DateTimeOffset.UtcNow, "{}");
            _revisions.Add(revision.RevisionId, revision);
            _apparatus[apparatusId] = new(apparatusId, name, revision.RevisionId);
            var receipt = new ScientificOperationReceipt(operationId, "apparatus.publish", apparatusId, actorSid, DateTimeOffset.UtcNow, System.Text.Json.JsonSerializer.Serialize(revision));
            _operations.Add(operationId, receipt);
            return Task.FromResult((revision, receipt));
        }

        public Task<IReadOnlyList<ScientificApparatusSummary>> ListAsync(string actorSid, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ScientificApparatusSummary>>(_apparatus.Values.ToList());
        public Task<ScientificApparatusSummary?> ReadLatestAsync(Guid apparatusId, string actorSid, CancellationToken ct = default) => Task.FromResult(_apparatus.GetValueOrDefault(apparatusId));
        public Task<IReadOnlyList<ScientificOperationReceipt>> HistoryAsync(Guid apparatusId, string actorSid, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ScientificOperationReceipt>>(_operations.Values.Where(value => value.ResourceId == apparatusId).ToList());
        public Task<ScientificRevision?> ReadAsync(Guid apparatusId, Guid revisionId, string actorSid, CancellationToken ct = default) => Task.FromResult(_revisions.GetValueOrDefault(revisionId));
        public Task<ScientificOperationReceipt?> OperationAsync(Guid operationId, string actorSid, CancellationToken ct = default) => Task.FromResult(_operations.GetValueOrDefault(operationId));
    }
}
