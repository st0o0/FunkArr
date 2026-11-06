using Akka.Hosting;
using Akka.TestKit.Xunit;
using FunkArr.ArrApi.Newznab.Requests;
using FunkArr.ArrApi.Newznab.Services;
using FunkArr.Core.Actors;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FunkArr.ArrApi.Tests;

public sealed class NewznabFeedProbeTests : TestKit
{
    private readonly Akka.TestKit.TestProbe _feedProbe;

    public NewznabFeedProbeTests()
    {
        _feedProbe = CreateTestProbe();
        ActorRegistry.For(Sys).Register<IFeedStoreManager>(_feedProbe);
    }

    [Fact]
    public async Task Feed_without_entries_of_requested_type_answers_with_cacheable_validation_item()
    {
        var req = Request("movie", cat: "2030,2040");

        var (query, result) = await Search(req, new FeedResult([], 3, 0));

        var success = Assert.IsType<SearchServiceResult.Success>(result);
        var item = Assert.Single(success.Rss.Channel.Items);
        Assert.Multiple(
            () => Assert.Equal(MediaType.Movie, query.MediaType),
            () => Assert.Equal(ValidationRss.MovieTitle, item.Title),
            () => Assert.Equal(1, success.Rss.Channel.Response.Total),
            () => Assert.True(success.Cacheable));
    }

    [Fact]
    public async Task Reported_total_is_the_filtered_total()
    {
        var req = Request("tvsearch", cat: "5030,5040");
        var entries = new[] { Entry("a"), Entry("b") };

        var (_, result) = await Search(req, new FeedResult(entries, 7, 2));

        var success = Assert.IsType<SearchServiceResult.Success>(result);
        Assert.Multiple(
            () => Assert.Equal(new[] { "a", "b" }, success.Rss.Channel.Items.Select(i => i.Guid.Value).ToArray()),
            () => Assert.Equal(2, success.Rss.Channel.Response.Total));
    }

    [Fact]
    public async Task Offset_beyond_last_matching_entry_answers_with_empty_page_and_filtered_total()
    {
        var req = Request("tvsearch", cat: "5030,5040", offset: 5);

        var (query, result) = await Search(req, new FeedResult([], 8, 5));

        var success = Assert.IsType<SearchServiceResult.Success>(result);
        Assert.Multiple(
            () => Assert.Equal(5, query.Offset),
            () => Assert.Empty(success.Rss.Channel.Items),
            () => Assert.Equal(5, success.Rss.Channel.Response.Total));
    }

    private async Task<(QueryFeed Query, SearchServiceResult Result)> Search(IndexerRequest req, FeedResult reply)
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new NewznabSearchService(
            ActorRegistry.For(Sys), cache, Options.Create(new ArrApiOptions()),
            TimeProvider.System, NullLogger<NewznabSearchService>.Instance);

        var task = service.Search(req, "http://localhost:6969", "key", TestContext.Current.CancellationToken);

        var query = _feedProbe.ExpectMsg<QueryFeed>(cancellationToken: TestContext.Current.CancellationToken);
        _feedProbe.Reply(reply);

        return (query, await task);
    }

    private static IndexerRequest Request(string t, string? cat = null, int offset = 0) =>
        new(t, Q: null, cat, offset, Limit: 100, Id: null, Season: null, Ep: null, TvdbId: null, ImdbId: null, TmdbId: null);

    private static FeedEntry Entry(string id) =>
        new(id, new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero), MediaType.Show,
            $"Probe.{id}.S2026E18.GERMAN.720p.WEB.h264-FunkArr", "ARD", "Probe",
            "https://example.com/video.mp4", 5400, 500_000_000, 720, null, 0.9, null,
            null, "2026", "18");
}
