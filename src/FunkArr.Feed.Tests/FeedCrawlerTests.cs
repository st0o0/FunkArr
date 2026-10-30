using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Search;
using FunkArr.Messages.Shared;

namespace FunkArr.Feed.Tests;

public sealed class FeedCrawlerTests : TestKit
{
    private const long _listTimestamp = 1_761_900_000;

    private static readonly FeedOptions _options = new()
    {
        PageSize = 2,
        MaxPages = 3,
        InitialLookback = TimeSpan.FromHours(48),
        MinConfidence = 0.7,
        MaxConcurrency = 1
    };

    private sealed record Probes(
        Akka.TestKit.TestProbe Mediathek,
        Akka.TestKit.TestProbe Resolver,
        Akka.TestKit.TestProbe Tv,
        Akka.TestKit.TestProbe Movie,
        Akka.TestKit.TestProbe Feed);

    private (FeedCrawler Crawler, Probes Probes) Create()
    {
        var probes = new Probes(CreateTestProbe(), CreateTestProbe(), CreateTestProbe(), CreateTestProbe(), CreateTestProbe());
        var crawler = new FeedCrawler(
            new FeedCrawlerActors(probes.Mediathek, probes.Resolver, probes.Tv, probes.Movie, probes.Feed),
            TimeProvider.System);
        return (crawler, probes);
    }

    [Fact]
    public async Task Unchanged_list_costs_one_query()
    {
        var (crawler, p) = Create();

        var run = crawler.RunAsync(_listTimestamp, _options, includeFuture: true, CancellationToken.None);

        var probe = p.Mediathek.ExpectMsg<QueryMediathek>();
        Assert.Multiple(
            () => Assert.Equal(1, probe.Size),
            () => Assert.Equal("filmlisteTimestamp", probe.SortBy),
            () => Assert.True(probe.Future));
        p.Mediathek.Reply(new QueryMediathekCompleted([], 0, _listTimestamp));

        var outcome = await run;

        Assert.Equal(FeedRunStatus.Unchanged, outcome.Status);
        p.Mediathek.ExpectNoMsg(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public async Task Run_publishes_matched_entries_and_counts_unmatched()
    {
        var (crawler, p) = Create();
        var tatort = TestData.Item("Herz aus Eis", "Tatort", _listTimestamp, url: "https://example.com/tatort.mp4");
        var unknown = TestData.Item("Irgendwas", "Unbekannt", _listTimestamp - 10);
        var old = TestData.Item("Alt", "Tatort", _listTimestamp - 400_000);

        var run = crawler.RunAsync(0, _options, includeFuture: true, CancellationToken.None);

        p.Mediathek.ExpectMsg<QueryMediathek>();
        p.Mediathek.Reply(new QueryMediathekCompleted([], 0, _listTimestamp));

        var firstPage = p.Mediathek.ExpectMsg<QueryMediathek>();
        Assert.Equal(0, firstPage.Offset);
        p.Mediathek.Reply(new QueryMediathekCompleted([tatort, unknown], 3, _listTimestamp));

        var secondPage = p.Mediathek.ExpectMsg<QueryMediathek>();
        Assert.Equal(2, secondPage.Offset);
        p.Mediathek.Reply(new QueryMediathekCompleted([old], 3, _listTimestamp));

        p.Resolver.ExpectMsg<QueryRegisteredRuleSets>();
        p.Resolver.Reply(new RegisteredRuleSetsResult(
            [new RegisteredRuleSetEntry("tatort", "Tatort", [], new ExternalIds(76290, "tt0806910", null), "Tatort", MediaType.Show)]));

        var evaluation = p.Tv.ExpectMsg<EvaluateSeries>();
        Assert.Multiple(
            () => Assert.Equal(SearchSource.Feed, evaluation.Source),
            () => Assert.Equal(76290, evaluation.TvdbId),
            () => Assert.Equal("tt0806910", evaluation.ImdbId),
            () => Assert.Equal("Herz aus Eis", Assert.Single(evaluation.Items).Title));
        p.Tv.Reply(new EvaluateSeriesCompleted(evaluation.SearchId, [TestData.Result("https://example.com/tatort.mp4")], 1));

        var add = p.Feed.ExpectMsg<AddFeedEntries>();
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(_listTimestamp), Assert.Single(add.Entries).FirstSeenAt);
        p.Feed.Reply(new AddFeedEntriesCompleted(1));

        var outcome = await run;

        Assert.Multiple(
            () => Assert.Equal(FeedRunStatus.Completed, outcome.Status),
            () => Assert.Equal(_listTimestamp, outcome.ListTimestamp),
            () => Assert.Equal(2, outcome.NewItems),
            () => Assert.Equal(1, outcome.Published),
            () => Assert.Equal(1, outcome.Unmatched),
            () => Assert.False(outcome.Gap));
    }

    [Fact]
    public async Task Failed_group_fails_the_run()
    {
        var (crawler, p) = Create();
        var tatort = TestData.Item("Herz aus Eis", "Tatort", _listTimestamp);

        var run = crawler.RunAsync(0, _options, includeFuture: true, CancellationToken.None);

        p.Mediathek.ExpectMsg<QueryMediathek>();
        p.Mediathek.Reply(new QueryMediathekCompleted([], 0, _listTimestamp));
        p.Mediathek.ExpectMsg<QueryMediathek>();
        p.Mediathek.Reply(new QueryMediathekCompleted([tatort], 1, _listTimestamp));
        p.Resolver.ExpectMsg<QueryRegisteredRuleSets>();
        p.Resolver.Reply(new RegisteredRuleSetsResult(
            [new RegisteredRuleSetEntry("tatort", "Tatort", [], new ExternalIds(null, null, null), "Tatort", MediaType.Show)]));
        var evaluation = p.Tv.ExpectMsg<EvaluateSeries>();
        p.Tv.Reply(new EvaluateSeriesFailed(evaluation.SearchId, new InvalidOperationException("boom")));

        var outcome = await run;

        Assert.Multiple(
            () => Assert.Equal(FeedRunStatus.Failed, outcome.Status),
            () => Assert.Equal(_listTimestamp, outcome.ListTimestamp),
            () => Assert.Equal("1 of 1 ruleset groups failed", outcome.Error));
    }

    [Fact]
    public async Task Mediathek_failure_fails_the_run_without_list_timestamp()
    {
        var (crawler, p) = Create();

        var run = crawler.RunAsync(0, _options, includeFuture: false, CancellationToken.None);

        p.Mediathek.ExpectMsg<QueryMediathek>();
        p.Mediathek.Reply(new QueryMediathekQueueFull());

        var outcome = await run;

        Assert.Multiple(
            () => Assert.Equal(FeedRunStatus.Failed, outcome.Status),
            () => Assert.Equal(0, outcome.ListTimestamp),
            () => Assert.Equal("MediathekViewWeb queue full", outcome.Error));
    }
}
