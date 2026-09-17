using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Extensions;
using FunkArr.Core;
using FunkArr.Messages.Mediathek;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ApiModels = FunkArr.Api.Models;

namespace FunkArr.Api;

public static class MediathekApiEndpoints
{
    private static readonly TimeSpan _queryTimeout = TimeSpan.FromSeconds(15);

    public static WebApplication MapMediathekApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/mediathek")
            .WithTags("Mediathek")
            .AddEndpointFilter<EndpointExceptionFilter>();

        group.MapGet("/search", async (
            string? q,
            string? channel,
            string? topic,
            int? durationMin,
            int? durationMax,
            int? offset,
            int? limit,
            string? sortBy,
            string? sortOrder,
            IActorRegistry registry) =>
        {
            if (string.IsNullOrWhiteSpace(q) && string.IsNullOrWhiteSpace(channel) && string.IsNullOrWhiteSpace(topic))
            {
                return Results.BadRequest(new ApiModels.ErrorResponse("At least one of q, channel, or topic is required"));
            }

            var actualLimit = Math.Clamp(limit ?? 20, 1, 100);
            var actualOffset = Math.Max(offset ?? 0, 0);

            var fields = new List<MediathekQueryField>();
            if (!string.IsNullOrWhiteSpace(q))
            {
                fields.Add(new MediathekQueryField(["title", "topic"], q));
            }

            if (!string.IsNullOrWhiteSpace(channel))
            {
                fields.Add(new MediathekQueryField(["channel"], channel));
            }

            if (!string.IsNullOrWhiteSpace(topic))
            {
                fields.Add(new MediathekQueryField(["topic"], topic));
            }

            var manager = await registry.GetAsync<IMediathekManager>();
            var query = new QueryMediathek(
                fields.ToArray(),
                SortBy: sortBy,
                SortOrder: sortOrder,
                Future: false,
                Offset: actualOffset,
                Size: actualLimit,
                DurationMin: durationMin,
                DurationMax: durationMax);

            var result = await manager.Ask<QueryMediathekResponse>(query, _queryTimeout);
            return result switch
            {
                QueryMediathekCompleted completed => Results.Ok(
                    new ApiModels.MediathekSearchResponse(
                        completed.Items.Select(i => i.ToApi()).ToArray(),
                        completed.Total)),
                QueryMediathekFailed failed => Results.Problem(
                    statusCode: 502, title: "MediathekViewWeb Error", detail: failed.Cause.Message),
                _ => ApiResults.GatewayTimeout(),
            };
        })
        .WithSummary("Search Mediathek")
        .WithDescription("Searches MediathekViewWeb for media items by title, channel, or topic.")
        .Produces<ApiModels.MediathekSearchResponse>()
        .ProducesProblem(400)
        .ProducesProblem(502)
        .ProducesProblem(504);

        return app;
    }
}
