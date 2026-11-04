using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Extensions;
using FunkArr.Api.Infrastructure;
using FunkArr.Api.Models.Feed;
using FunkArr.Api.Models.Shared;
using FunkArr.Api.Validation;
using FunkArr.Core.Actors;
using FunkArr.Messages.Feed;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FunkArr.Api;

public static class FeedApiEndpoints
{
    private const int _defaultLimit = 100;
    private const int _maxLimit = 500;
    public static WebApplication MapFeedApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/feed")
            .WithTags("Feed")
            .AddEndpointFilter<ValidationEndpointFilter>()
            .AddEndpointFilter<EndpointExceptionFilter>();
        group.WithRequestTimeout(TimeSpan.FromSeconds(30));

        group.MapGet("/", async (
            int? limit, int? offset, MediaType? type, string? channel, int? quality, string? q,
            IActorRegistry registry, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? _defaultLimit, 1, _maxLimit);
            var skip = Math.Max(0, offset ?? 0);
            var mediaType = type?.ToMessage();
            var feed = await registry.GetAsync<IFeedStoreManager>(ct);
            var ingest = await registry.GetAsync<IFeedCrawlManager>(ct);

            var feedTask = feed.Ask<FeedResponse>(
                new QueryFeed(mediaType, skip, take, channel, quality, q), ct);
            var statusTask = ingest.Ask<FeedCrawlStatusResponse>(new QueryFeedCrawlStatus(), ct);
            var feedResponse = await feedTask;
            var statusResponse = await statusTask;

            return (feedResponse, statusResponse) switch
            {
                (FeedResult entries, FeedCrawlStatusResult status) => Results.Ok(entries.ToApi(status)),
                _ => Results.Problem(statusCode: 500, title: "Feed unavailable")
            };
        })
        .WithSummary("Get RSS feed overview")
        .WithDescription("Returns the newest releases published to the RSS feed and the status of the last feed ingest run.")
        .Produces<FeedOverviewResponse>()
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

}
