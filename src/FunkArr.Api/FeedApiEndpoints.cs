using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Extensions;
using FunkArr.Api.Validation;
using FunkArr.Core;
using FunkArr.Messages.Feed;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ApiModels = FunkArr.Api.Models;

namespace FunkArr.Api;

public static class FeedApiEndpoints
{
    private const int _defaultLimit = 100;
    private const int _maxLimit = 500;
    private static readonly TimeSpan _askTimeout = TimeSpan.FromSeconds(10);

    public static WebApplication MapFeedApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/feed")
            .WithTags("Feed")
            .AddEndpointFilter<ValidationEndpointFilter>()
            .AddEndpointFilter<EndpointExceptionFilter>();
        group.WithRequestTimeout(TimeSpan.FromSeconds(30));

        group.MapGet("", async (
            int? limit, int? offset, string? type, string? channel, int? quality, string? q,
            IActorRegistry registry, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? _defaultLimit, 1, _maxLimit);
            var skip = Math.Max(0, offset ?? 0);
            var mediaType = ParseMediaType(type);
            var feed = await registry.GetAsync<IFeedStoreManager>(ct);
            var ingest = await registry.GetAsync<IFeedCrawlManager>(ct);

            var feedTask = feed.Ask<FeedResponse>(
                new QueryFeed(mediaType, skip, take, channel, quality, q), _askTimeout, ct);
            var statusTask = ingest.Ask<FeedCrawlStatusResponse>(new QueryFeedCrawlStatus(), _askTimeout, ct);
            var feedResponse = await feedTask;
            var statusResponse = await statusTask;

            return (feedResponse, statusResponse) switch
            {
                (FeedResult entries, FeedCrawlStatusResult status) => Results.Ok(ToApi(entries, status)),
                _ => Results.Problem(statusCode: 500, title: "Feed unavailable")
            };
        })
        .WithSummary("Get RSS feed overview")
        .WithDescription("Returns the newest releases published to the RSS feed and the status of the last feed ingest run.")
        .Produces<ApiModels.FeedOverviewResponse>()
        .ProducesProblem(500)
        .ProducesProblem(504);

        group.MapPost("/crawl", async (IActorRegistry registry, CancellationToken ct) =>
        {
            var ingest = await registry.GetAsync<IFeedCrawlManager>(ct);
            ingest.Tell(new StartFeedCrawl());
            return Results.Accepted();
        })
        .WithSummary("Start a feed ingest run")
        .WithDescription("Requests an immediate feed ingest run. Ignored while a run is in progress.")
        .Produces(StatusCodes.Status202Accepted);

        return app;
    }

    internal static ApiModels.FeedOverviewResponse ToApi(FeedResult entries, FeedCrawlStatusResult status) =>
        new(
            [
                .. entries.Entries.Select(e => new ApiModels.FeedEntryOutput(
                    e.StableId,
                    e.FirstSeenAt,
                    e.MediaType.ToApi(),
                    e.Title,
                    e.Channel,
                    e.Topic,
                    e.Quality,
                    e.Season,
                    e.Episode))
            ],
            entries.Total,
            entries.FilteredTotal,
            new ApiModels.FeedCrawlStatusOutput(
                status.Enabled,
                status.Running,
                status.LastRunAt,
                status.LastOutcome,
                status.LastNewItems,
                status.LastPublished,
                status.LastUnmatched,
                status.LastGap,
                status.ConsecutiveFailures));

    private static Messages.MediaType? ParseMediaType(string? type) => type?.ToLowerInvariant() switch
    {
        "show" => Messages.MediaType.Show,
        "movie" => Messages.MediaType.Movie,
        _ => null
    };
}
