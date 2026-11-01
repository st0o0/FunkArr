using System.Collections.Concurrent;
using Akka.Streams;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Search;
using FunkArr.Messages.Shared;

namespace FunkArr.Feed.Tests;

public sealed class FeedCrawlPipelineTests : TestKit
{
    private const long _listTimestamp = 1_761_900_000;
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    private static readonly FeedOptions _options = Options();

    private static FeedOptions Options(int maxPages = 3) => new()
    {
        PageSize = 2,
        MaxPages = maxPages,
        InitialLookback = TimeSpan.FromHours(48),
        MaxCatchUp = TimeSpan.FromDays(7),
        MinConfidence = 0.7,
        MaxConcurrency = 2
    };

    private static readonly RegisteredRuleSetEntry _tatort =
        new("tatort", "Tatort", [], new ExternalIds(76290, "tt0806910", null), "Tatort", MediaType.Show);

    private readonly ConcurrentQueue<(int Offset, int Size)> _queries = new();
    private readonly ConcurrentQueue<RuleSetGroup> _evaluated = new();
    private readonly ConcurrentQueue<FeedEntry[]> _published = new();

    private FeedCrawlStages Stages(
        Func<int, int, QueryMediathekResponse> page,
        Func<RuleSetGroup, SearchResultItem[]>? evaluate = null) =>
        new(
            QueryPage: (offset, size, _) =>
            {
                _queries.Enqueue((offset, size));
                return Task.FromResult(page(offset, size));
            },
            LoadRuleSets: _ => Task.FromResult(new[] { _tatort }),
            Evaluate: (group, _) =>
            {
                _evaluated.Enqueue(group);
                return Task.FromResult(evaluate?.Invoke(group) ?? []);
            },
            Publish: (entries, _) =>
            {
                _published.Enqueue(entries);
                return Task.FromResult(entries.Length);
            });

    private async Task<FeedRunOutcome> Run(FeedCrawlStages stages, long cursor = 0, FeedOptions? options = null) =>
        await new FeedCrawlPipeline(stages, TimeProvider.System)
            .Run(cursor, options ?? _options, Sys.Materializer())
            .Completion.WaitAsync(_timeout);

    private static Func<int, int, QueryMediathekResponse> Pages(params MediathekItem[][] pages) =>
        (offset, size) => size == 1
            ? new QueryMediathekCompleted([], 0, _listTimestamp)
            : new QueryMediathekCompleted(offset / size < pages.Length ? pages[offset / size] : [], 100, _listTimestamp);

    [Fact]
    public async Task Unchanged_list_costs_one_query()
    {
        var outcome = await Run(Stages(Pages()), cursor: _listTimestamp);

        Assert.Multiple(
            () => Assert.Equal(FeedRunStatus.Unchanged, outcome.Status),
            () => Assert.Equal(_listTimestamp, outcome.ListTimestamp),
            () => Assert.Equal((0, 1), Assert.Single(_queries)),
            () => Assert.Empty(_evaluated));
    }

    [Fact]
    public async Task Run_publishes_matched_entries_and_counts_unmatched()
    {
        var tatort = TestData.Item("Herz aus Eis", "Tatort", _listTimestamp, url: "https://example.com/tatort.mp4");
        var unknown = TestData.Item("Irgendwas", "Unbekannt", _listTimestamp - 10);
        var old = TestData.Item("Alt", "Tatort", _listTimestamp - 400_000);
        var stages = Stages(Pages([tatort, unknown], [old]), _ => [TestData.Result("https://example.com/tatort.mp4")]);

        var outcome = await Run(stages);

        var group = Assert.Single(_evaluated);
        var entry = Assert.Single(Assert.Single(_published));
        Assert.Multiple(
            () => Assert.Equal(FeedRunStatus.Completed, outcome.Status),
            () => Assert.Equal(_listTimestamp, outcome.ListTimestamp),
            () => Assert.Equal(2, outcome.NewItems),
            () => Assert.Equal(1, outcome.Published),
            () => Assert.Equal(1, outcome.Unmatched),
            () => Assert.False(outcome.Gap),
            () => Assert.Equal("tatort", group.RuleSetId),
            () => Assert.Equal(76290, group.Ids.TvdbId),
            () => Assert.Equal("Herz aus Eis", Assert.Single(group.Items).Title),
            () => Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(_listTimestamp), entry.FirstSeenAt));
    }

    [Fact]
    public async Task Failed_group_fails_the_run()
    {
        var tatort = TestData.Item("Herz aus Eis", "Tatort", _listTimestamp);
        var stages = Stages(Pages([tatort])) with
        {
            Evaluate = (_, _) => Task.FromException<SearchResultItem[]>(new InvalidOperationException("boom"))
        };

        var outcome = await Run(stages);

        Assert.Multiple(
            () => Assert.Equal(FeedRunStatus.Failed, outcome.Status),
            () => Assert.Equal(_listTimestamp, outcome.ListTimestamp),
            () => Assert.Equal("1 of 1 ruleset groups failed", outcome.Error),
            () => Assert.Empty(_published));
    }

    [Fact]
    public async Task Failed_publish_fails_the_run()
    {
        var tatort = TestData.Item("Herz aus Eis", "Tatort", _listTimestamp, url: "https://example.com/tatort.mp4");
        var stages = Stages(Pages([tatort]), _ => [TestData.Result("https://example.com/tatort.mp4")]) with
        {
            Publish = (_, _) => Task.FromException<int>(new InvalidOperationException("store down"))
        };

        var outcome = await Run(stages);

        Assert.Multiple(
            () => Assert.Equal(FeedRunStatus.Failed, outcome.Status),
            () => Assert.Equal("publishing feed entries failed: store down", outcome.Error));
    }

    [Fact]
    public async Task Mediathek_queue_full_after_retries_fails_the_run_without_list_timestamp()
    {
        var outcome = await Run(Stages((_, _) => new QueryMediathekQueueFull()));

        Assert.Multiple(
            () => Assert.Equal(FeedRunStatus.Failed, outcome.Status),
            () => Assert.Equal(0, outcome.ListTimestamp),
            () => Assert.Equal("MediathekViewWeb queue full", outcome.Error),
            () => Assert.Equal(FeedCrawlPipeline.MaxQueueRetries + 1, _queries.Count));
    }

    [Fact]
    public async Task Page_limit_without_reaching_the_cursor_is_a_gap()
    {
        var full = new[]
        {
            TestData.Item("a", "Tatort", _listTimestamp - 1),
            TestData.Item("b", "Tatort", _listTimestamp - 2)
        };

        var outcome = await Run(Stages(Pages(full)), options: Options(maxPages: 1));

        Assert.Multiple(
            () => Assert.Equal(FeedRunStatus.Completed, outcome.Status),
            () => Assert.True(outcome.Gap),
            () => Assert.Equal(2, outcome.NewItems));
    }

    [Fact]
    public async Task Long_outage_truncates_the_window_and_reports_a_gap()
    {
        var recent = TestData.Item("a", "Tatort", _listTimestamp - 10);

        var outcome = await Run(Stages(Pages([recent])), cursor: 1_000);

        Assert.Multiple(
            () => Assert.Equal(FeedRunStatus.Completed, outcome.Status),
            () => Assert.True(outcome.Gap),
            () => Assert.Equal(1, outcome.NewItems));
    }

    [Fact]
    public async Task Prefetch_stops_after_the_page_that_reaches_the_cursor()
    {
        var old = TestData.Item("Alt", "Tatort", _listTimestamp - 400_000);

        await Run(Stages(Pages([old, old], [old, old], [old, old], [old, old])), options: Options(maxPages: 20));

        var pageQueries = _queries.Count(q => q.Size > 1);
        Assert.InRange(pageQueries, 1, FeedCrawlPipeline.PagePrefetch + 1);
    }

    [Fact]
    public async Task Cancel_aborts_a_running_crawl()
    {
        var never = new TaskCompletionSource<QueryMediathekResponse>();
        var stages = Stages(Pages()) with { QueryPage = (_, _, _) => never.Task };
        var run = new FeedCrawlPipeline(stages, TimeProvider.System).Run(0, _options, Sys.Materializer());

        run.Cancel();

        var ex = await Assert.ThrowsAsync<AggregateException>(() => run.Completion.WaitAsync(_timeout));
        var cancelled = Assert.IsType<OperationCanceledException>(Assert.Single(ex.InnerExceptions), exactMatch: false);
        Assert.Equal("Feed crawl cancelled", cancelled.Message);
    }
}
