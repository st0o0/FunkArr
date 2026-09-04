using Akka.Actor;
using Akka.Hosting;
using FunkArr.Core;
using FunkArr.Messages.Mediathek;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FunkArr.Api;

public static class MediathekApiEndpoints
{
    private static readonly TimeSpan _queryTimeout = TimeSpan.FromSeconds(15);

    public static WebApplication MapMediathekApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/mediathek");

        group.MapGet("/search", async (string? q, int? limit, IActorRegistry registry) =>
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Results.BadRequest(new { error = "q parameter is required" });
            }

            var actualLimit = Math.Clamp(limit ?? 20, 1, 100);

            var manager = await registry.GetAsync<IMediathekManager>();
            try
            {
                var query = new QueryMediathek(
                    [new MediathekQueryField(["title", "topic"], q)],
                    SortBy: null,
                    SortOrder: null,
                    Future: false,
                    Offset: 0,
                    Size: actualLimit,
                    DurationMin: null,
                    DurationMax: null);

                var result = await manager.Ask<IMediathekResponse>(query, _queryTimeout);
                return result switch
                {
                    MediathekQueryCompleted completed => Results.Ok(
                        completed.Items.Select(item => new MediathekSearchResult(
                            item.Title,
                            item.Topic,
                            item.Channel,
                            item.Duration,
                            EstimateQuality(item),
                            item.Description,
                            item.Timestamp)).ToArray()),
                    MediathekQueryFailed failed => Results.Problem(
                        statusCode: 502, title: "MediathekViewWeb Error", detail: failed.Reason),
                    _ => Results.Problem(statusCode: 504, title: "Gateway Timeout"),
                };
            }
            catch (Exception)
            {
                return Results.Problem(statusCode: 504, title: "Gateway Timeout");
            }
        })
        .Produces<MediathekSearchResult[]>()
        .ProducesProblem(400)
        .ProducesProblem(502)
        .ProducesProblem(504);

        return app;
    }

    private static int EstimateQuality(MediathekItem item)
    {
        if (item.UrlVideoHd is not null)
        {
            return 1080;
        }

        if (item.UrlVideo is not null)
        {
            return 720;
        }

        if (item.UrlVideoLow is not null)
        {
            return 480;
        }

        return 0;
    }

    private sealed record MediathekSearchResult(
        string Title,
        string Topic,
        string Channel,
        int Duration,
        int Quality,
        string? Description,
        long Timestamp);
}
