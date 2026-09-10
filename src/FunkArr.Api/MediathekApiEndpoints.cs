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
                return Results.BadRequest(new { error = "At least one of q, channel, or topic is required" });
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
            try
            {
                var query = new QueryMediathek(
                    fields.ToArray(),
                    SortBy: sortBy,
                    SortOrder: sortOrder,
                    Future: false,
                    Offset: actualOffset,
                    Size: actualLimit,
                    DurationMin: durationMin,
                    DurationMax: durationMax);

                var result = await manager.Ask<IMediathekResponse>(query, _queryTimeout);
                return result switch
                {
                    MediathekQueryCompleted completed => Results.Ok(
                        new MediathekSearchResponse(
                            completed.Items.Select(ToSearchResult).ToArray(),
                            completed.Total)),
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
        .Produces<MediathekSearchResponse>()
        .ProducesProblem(400)
        .ProducesProblem(502)
        .ProducesProblem(504);

        return app;
    }

    internal static MediathekSearchResult ToSearchResult(MediathekItem item) =>
        new(item.Title,
            item.Topic,
            item.Channel,
            item.Duration,
            EstimateQuality(item),
            item.Description,
            item.Timestamp,
            item.Size,
            item.UrlSubtitle is not null,
            item.UrlVideoHd is not null,
            item.UrlWebsite);

    internal static int EstimateQuality(MediathekItem item)
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

    internal sealed record MediathekSearchResponse(
        MediathekSearchResult[] Items,
        int TotalResults);

    internal sealed record MediathekSearchResult(
        string Title,
        string Topic,
        string Channel,
        int Duration,
        int Quality,
        string? Description,
        long Timestamp,
        long Size,
        bool HasSubtitles,
        bool HasHd,
        string? WebsiteUrl);
}
