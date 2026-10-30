using Helix.Orchestration.SiteServer.Campaigns;
using Helix.Orchestration.SiteServer.Contracts;

namespace Helix.Orchestration.SiteServer.Endpoints;

public static class CampaignEndpoints
{
    public static void MapCampaignEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/campaigns")
            .WithTags("Campaigns");

        group.MapPost("/", async Task<IResult> (
            CreateCampaignRequest request,
            ICampaignManager campaignManager,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new { Error = "Campaign name is required." });

            var campaign = await campaignManager.CreateAsync(
                request.Name,
                request.Description,
                request.ParameterSets,
                ct);

            return Results.Created($"/api/campaigns/{campaign.CampaignId}", ToResponse(campaign));
        })
        .WithName("CreateCampaign")
        .WithSummary("Create a new campaign definition")
        .Produces<CampaignResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/", async Task<IResult> (
            ICampaignManager campaignManager,
            CancellationToken ct) =>
        {
            var campaigns = await campaignManager.GetAllAsync(ct);
            return Results.Ok(campaigns.Select(ToResponse));
        })
        .WithName("GetCampaigns")
        .WithSummary("List all campaigns")
        .Produces<IEnumerable<CampaignResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{id}", async Task<IResult> (
            string id,
            ICampaignManager campaignManager,
            CancellationToken ct) =>
        {
            var campaign = await campaignManager.GetByIdAsync(id, ct);
            return campaign is null
                ? Results.NotFound(new { Error = $"Campaign '{id}' not found." })
                : Results.Ok(ToResponse(campaign));
        })
        .WithName("GetCampaign")
        .WithSummary("Get campaign by id")
        .Produces<CampaignResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{id}/start", async Task<IResult> (
            string id,
            ICampaignManager campaignManager,
            CancellationToken ct) =>
        {
            try
            {
                var campaign = await campaignManager.StartAsync(id, ct);
                return Results.Ok(ToResponse(campaign));
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new { Error = $"Campaign '{id}' not found." });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to start campaign",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("StartCampaign")
        .WithSummary("Start campaign execution")
        .Produces<CampaignResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapDelete("/{id}", async Task<IResult> (
            string id,
            ICampaignManager campaignManager,
            CancellationToken ct) =>
        {
            var deleted = await campaignManager.DeleteAsync(id, ct);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { Error = $"Campaign '{id}' not found." });
        })
        .WithName("DeleteCampaign")
        .WithSummary("Delete campaign")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }

    private static CampaignResponse ToResponse(Campaign campaign)
        => new()
        {
            CampaignId = campaign.CampaignId,
            Name = campaign.Name,
            Description = campaign.Description,
            Status = campaign.Status.ToString(),
            CreatedAt = campaign.CreatedAt,
            StartedAt = campaign.StartedAt,
            EndedAt = campaign.EndedAt,
            Iterations = campaign.Iterations
                .OrderBy(i => i.Index)
                .Select(i => new CampaignIterationResponse
                {
                    Index = i.Index,
                    Parameters = new Dictionary<string, object?>(i.Parameters),
                    RunspaceId = i.RunspaceId,
                    Status = i.Status.ToString(),
                    Message = i.Message,
                    CreatedAt = i.CreatedAt,
                    StartedAt = i.StartedAt,
                    EndedAt = i.EndedAt
                })
                .ToList()
        };
}
