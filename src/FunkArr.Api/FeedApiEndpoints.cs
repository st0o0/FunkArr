using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Validation;
using FunkArr.Core;
using FunkArr.Messages.Feed;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
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

        group.MapGet("", async (int? limit, IActorRegistry registry, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? _defaultLimit, 1, _maxLimit);
            var feed = await registry.GetAsync<IFeedManager>(ct);
            var ingest = await registry.GetAsync<IFeedIngestManager>(ct);

            var feedTask = feed.Ask<FeedResponse>(new QueryFeed(null, 0, take), _askTimeout, ct);
            var statusTask = ingest.Ask<FeedIngestStatusResponse>(new QueryFeedIngestStatus(), _askTimeout, ct);
            var feedResponse = await feedTask;
            var statusResponse = await statusTask;

            return (feedResponse, statusResponse) switch
            {
                (FeedResult entries, FeedIngestStatusResult status) => Results.Ok(ToApi(entries, status)),
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
            var ingest = await registry.GetAsync<IFeedIngestManager>(ct);
            ingest.Tell(new TriggerFeedCrawl());
            return Results.Accepted();
        })
        .WithSummary("Start a feed ingest run")
        .WithDescription("Requests an immediate feed ingest run. Ignored while a run is in progress.")
        .Produces(StatusCodes.Status202Accepted);

        return app;
    }

    internal static ApiModels.FeedOverviewResponse ToApi(FeedResult entries, FeedIngestStatusResult status) =>
        new(
            [
                .. entries.Entries.Select(e => new ApiModels.FeedEntryOutput(
                    e.StableId,
                    e.FirstSeenAt,
                    (ApiModels.MediaType)(int)e.MediaType,
                    e.Item.Title,
                    e.Item.Channel,
                    e.Item.Topic,
                    e.Item.Quality,
                    e.Item.Metadata?.Season,
                    e.Item.Metadata?.Episode))
            ],
            entries.Total,
            new ApiModels.FeedIngestStatusOutput(
                status.Enabled,
                status.Running,
                status.LastRunAt,
                status.LastOutcome,
                status.LastNewItems,
                status.LastPublished,
                status.LastUnmatched,
                status.LastGap,
                status.ConsecutiveFailures));
}
