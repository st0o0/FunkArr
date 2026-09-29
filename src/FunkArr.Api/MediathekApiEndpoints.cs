using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Extensions;
using FunkArr.Api.Validation;
using FunkArr.Core;
using FunkArr.Messages.Mediathek;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using ApiModels = FunkArr.Api.Models;

namespace FunkArr.Api;

public static class MediathekApiEndpoints
{
    private static readonly TimeSpan _queryTimeout = TimeSpan.FromSeconds(15);

    public static WebApplication MapMediathekApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/mediathek")
            .WithTags("Mediathek")
            .AddEndpointFilter<ValidationEndpointFilter>()
            .AddEndpointFilter<EndpointExceptionFilter>();
        group.WithRequestTimeout(TimeSpan.FromSeconds(60));

        group.MapGet("/search", async ([AsParameters] ApiModels.MediathekSearchRequest req, IActorRegistry registry, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Q) && string.IsNullOrWhiteSpace(req.Channel) && string.IsNullOrWhiteSpace(req.Topic))
            {
                return Results.BadRequest(new ApiModels.ErrorResponse("At least one of q, channel, or topic is required"));
            }

            var actualLimit = req.Limit ?? 20;
            var actualOffset = req.Offset ?? 0;

            var fields = new List<MediathekQueryField>();
            if (!string.IsNullOrWhiteSpace(req.Q))
            {
                fields.Add(new MediathekQueryField(["title", "topic"], req.Q));
            }

            if (!string.IsNullOrWhiteSpace(req.Channel))
            {
                fields.Add(new MediathekQueryField(["channel"], req.Channel));
            }

            if (!string.IsNullOrWhiteSpace(req.Topic))
            {
                fields.Add(new MediathekQueryField(["topic"], req.Topic));
            }

            var manager = await registry.GetAsync<IMediathekManager>(ct);
            var query = new QueryMediathek(
                [.. fields],
                SortBy: req.SortBy,
                SortOrder: req.SortOrder,
                Future: false,
                Offset: actualOffset,
                Size: actualLimit,
                DurationMin: req.DurationMin,
                DurationMax: req.DurationMax);

            var result = await manager.Ask<QueryMediathekResponse>(query, _queryTimeout, ct);
            return result switch
            {
                QueryMediathekCompleted completed => Results.Ok(
                    new ApiModels.MediathekSearchResponse(
                        [.. completed.Items.Select(i => i.ToApi())],
                        completed.Total)),
                QueryMediathekQueueFull => Results.Problem(
                    statusCode: 503, title: "Service Busy", detail: "MediathekViewWeb query queue is full"),
                QueryMediathekError error => Results.Problem(
                    statusCode: 502, title: "MediathekViewWeb Error", detail: error.Cause.Message),
                _ => ApiResults.GatewayTimeout()
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
