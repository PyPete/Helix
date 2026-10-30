using Helix.Orchestration.Core.Trends;
using Helix.Orchestration.SiteServer.Contracts;

namespace Helix.Orchestration.SiteServer.Endpoints;

/// <summary>
/// Minimal API endpoints for trend discovery and history retrieval.
/// </summary>
public static class TrendEndpoints
{
    public static void MapTrendEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/trends")
            .WithTags("Trends");

        // GET /api/trends — List all available trends
        group.MapGet("/", IResult (ITrendProvider trendProvider) =>
        {
            try
            {
                var trends = trendProvider.GetAllTrends()
                    .Select(t => new TrendDescriptorDto
                    {
                        TrendId = t.TrendId,
                        TrendName = t.TrendName,
                        TrendKind = t.TrendKind.ToString(),
                        Units = t.Units,
                        ExperimentId = t.ExperimentId,
                        Tags = t.Tags,
                        IsOnline = t.IsOnline,
                        LastUpdatedAt = t.LastUpdatedAt
                    });

                return Results.Ok(trends);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to retrieve trends",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("GetAllTrends")
        .WithSummary("List all available trends and their metadata")
        .Produces<IEnumerable<TrendDescriptorDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /api/trends/{id}/history — Retrieve historical values for a scalar trend
        group.MapGet("/{id}/history", IResult (
            string id,
            ITrendProvider trendProvider,
            int? maxPoints) =>
        {
            try
            {
                // Phase 9: assume double scalar trends for simplicity
                var trend = trendProvider.GetScalarTrend<double>(id);
                var window = trend.Window;

                var allValues = window.GetValues();
                var values = maxPoints.HasValue
                    ? allValues.TakeLast(maxPoints.Value).ToArray()
                    : allValues;

                var response = new TrendHistoryResponse<double>
                {
                    TrendId = trend.TrendId,
                    TrendName = trend.TrendName,
                    Values = values,
                    Count = values.Length
                };

                return Results.Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new { Error = $"Scalar trend '{id}' not found." });
            }
            catch (InvalidCastException)
            {
                return Results.BadRequest(new { Error = $"Trend '{id}' is not a scalar double trend." });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to retrieve trend history",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("GetTrendHistory")
        .WithSummary("Retrieve historical values for a scalar trend")
        .Produces<TrendHistoryResponse<double>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
