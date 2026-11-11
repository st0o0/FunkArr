using System.Security.Cryptography;
using System.Text;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Util;
using FunkArr.Core.Actors;
using FunkArr.Core.Configuration;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Shared;
using Terp.Akka;

namespace FunkArr.Feed.Crawl;

public sealed class FeedHarvestActor : ReceiveActor
{
    private static readonly TimeSpan _probeTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _resolveTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _pageTimeout = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan _evalTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _publishTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _maxLead = TimeSpan.FromDays(90);

    private readonly IActorRef _mediathek;
    private readonly IActorRef _ruleSetResolver;
    private readonly IActorRef _tvEvaluation;
    private readonly IActorRef _movieEvaluation;
    private readonly IActorRef _store;
    private readonly IMaterializer _materializer;
    private readonly TimeProvider _timeProvider;

    private sealed record StreamCompleted(HarvestAccumulator? Result, long StartedAt);
    private sealed record StreamFailed(string Reason);

    private sealed record Resolved(
        long Timestamp,
        long LowerBound,
        long UpperBound,
        bool Truncated,
        IReadOnlyDictionary<string, TopicEntry> Targets,
        int PageSize,
        int MaxPages,
        double MinConfidence);

    private sealed record TopicEntry(string RuleSetId, MediaType MediaType, ExternalIds Ids);

    public FeedHarvestActor(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _mediathek = Context.GetActor<IMediathekManager>();
        _ruleSetResolver = Context.GetActor<IRuleSetResolver>();
        _tvEvaluation = Context.GetActor<ITvEvaluationRegion>();
        _movieEvaluation = Context.GetActor<IMovieEvaluationRegion>();
        _store = Context.GetActor<IFeedStoreManager>();
        _materializer = Context.Materializer();

        Receive<StartHarvest>(msg =>
        {
            var replyTo = Sender;
            var cursor = msg.Cursor;
            var options = msg.Options;
            var startedAt = _timeProvider.GetTimestamp();

            var mediathek = _mediathek;
            var resolver = _ruleSetResolver;
            var tvEval = _tvEvaluation;
            var movieEval = _movieEvaluation;
            var store = _store;

            Source.Single(cursor)
                .SelectAsync(1, _ => mediathek.Ask<QueryMediathekResponse>(
                    new QueryMediathek([], "filmlisteTimestamp", "desc",
                        Future: true, 0, 1, 300, null), _probeTimeout))
                .Select(response => response switch
                {
                    QueryMediathekCompleted { ListTimestamp: > 0 } ok
                        when ok.ListTimestamp > cursor => ok.ListTimestamp,
                    _ => -1L
                })
                .Where(timestamp => timestamp > 0)
                .SelectAsync(1, async timestamp =>
                {
                    var result = await resolver.Ask<RegisteredRuleSetsResult>(
                        new QueryRegisteredRuleSets(), _resolveTimeout);

                    var targets = BuildTargetMap(result.Entries);
                    var (lower, upper, truncated) = ComputeWindow(
                        cursor, timestamp, options.MaxCatchUp, options.InitialLookback);
                    var pageSize = Math.Max(1, options.PageSize);
                    var maxPages = Math.Max(1, options.MaxPages);

                    return new Resolved(timestamp, lower, upper, truncated,
                        targets, pageSize, maxPages, options.MinConfidence);
                })
                .ConcatMany(ctx =>
                    Source.UnfoldAsync<int, MediathekItem[]>(0, async offset =>
                        {
                            if (offset < 0)
                            {
                                return Option<(int, MediathekItem[])>.None;
                            }

                            var response = await mediathek.Ask<QueryMediathekResponse>(
                                new QueryMediathek([], "filmlisteTimestamp", "desc",
                                    Future: true, offset, ctx.PageSize, 300, null),
                                _pageTimeout);

                            var (items, reachedEnd) = FilterPage(response, ctx);
                            var next = reachedEnd || offset / ctx.PageSize >= ctx.MaxPages - 1
                                ? -1
                                : offset + ctx.PageSize;

                            return Option<(int, MediathekItem[])>.Create((next, items));
                        })
                        .SelectMany(items => GroupByTopic(items, ctx.Targets))
                        .SelectAsyncUnordered(4, group =>
                            group.EvaluateAsync(
                                group.MediaType == MediaType.Movie ? movieEval : tvEval,
                                _evalTimeout))
                        .Select(scored => scored.Gate(ctx.MinConfidence))
                        .Aggregate(
                            new HarvestAccumulator(ctx.Timestamp, ctx.Truncated),
                            (acc, g) => acc.Add(g))
                        .SelectAsync(1, async acc =>
                        {
                            if (acc.HasEntries)
                            {
                                await store.Ask<PublishFeedEntriesResponse>(
                                    new PublishFeedEntries([.. acc.Accepted]), _publishTimeout);
                            }

                            return acc;
                        }))
                .RunWith(Sink.FirstOrDefault<HarvestAccumulator>(), _materializer)
                .PipeTo(replyTo,
                    success: acc => acc is not null
                        ? new HarvestCompleted(
                            acc.ToSummary(_timeProvider.GetElapsedTime(startedAt)))
                        : (HarvestResponse)new HarvestUnchanged(),
                    failure: ex => new HarvestFailed(ex.Message));
        });
    }

    private static IReadOnlyDictionary<string, TopicEntry> BuildTargetMap(
        IReadOnlyList<RegisteredRuleSetEntry> entries)
    {
        var map = new Dictionary<string, TopicEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var target = new TopicEntry(entry.RuleSetId,
                entry.MediaType ?? MediaType.Show, entry.Ids);

            foreach (var key in entry.Aliases.Prepend(entry.Topic))
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    map.TryAdd(key.Trim(), target);
                }
            }
        }

        return map;
    }

    private static (long lower, long upper, bool truncated) ComputeWindow(
        long cursor, long listTimestamp, TimeSpan maxCatchUp, TimeSpan initialLookback)
    {
        var catchUp = (long)maxCatchUp.TotalSeconds;

        if (cursor <= 0)
        {
            return (Math.Max(0, listTimestamp - (long)initialLookback.TotalSeconds), listTimestamp, false);
        }

        if (listTimestamp - catchUp is var earliest && cursor < earliest)
        {
            return (earliest, listTimestamp, true);
        }

        return (cursor, listTimestamp, false);
    }

    private static (MediathekItem[] Items, bool ReachedEnd) FilterPage(
        QueryMediathekResponse response, Resolved ctx)
    {
        if (response is not QueryMediathekCompleted completed)
        {
            return ([], true);
        }

        var leadLimit = (DateTimeOffset.UtcNow + _maxLead).ToUnixTimeSeconds();
        var items = new List<MediathekItem>(completed.Items.Length);
        var reachedEnd = completed.Items.Length < ctx.PageSize;

        foreach (var item in completed.Items)
        {
            if (item.FilmlisteTimestamp <= ctx.LowerBound)
            {
                reachedEnd = true;
                break;
            }

            if (item.FilmlisteTimestamp <= ctx.UpperBound)
            {
                var url = item.UrlVideoHd ?? item.UrlVideo ?? item.UrlVideoLow;
                if (url is not null && item.Timestamp <= leadLimit)
                {
                    items.Add(item);
                }
            }
        }

        return ([.. items], reachedEnd);
    }

    private static IReadOnlyList<ItemGroup> GroupByTopic(
        MediathekItem[] items,
        IReadOnlyDictionary<string, TopicEntry> targets)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var groups = new Dictionary<string, (TopicEntry Target, List<MediathekItem> Items)>(
            StringComparer.Ordinal);

        foreach (var item in items)
        {
            var url = item.UrlVideoHd ?? item.UrlVideo ?? item.UrlVideoLow;
            if (url is null)
            {
                continue;
            }

            var stableId = StableIdFor(item.Channel, url);
            if (!seen.Add(stableId))
            {
                continue;
            }

            if (!targets.TryGetValue(item.Topic, out var target))
            {
                continue;
            }

            if (!groups.TryGetValue(target.RuleSetId, out var group))
            {
                group = (target, []);
                groups[target.RuleSetId] = group;
            }

            group.Items.Add(item);
        }

        return [.. groups.Select(g =>
            new ItemGroup(g.Key, g.Value.Target.MediaType, g.Value.Target.Ids,
                [.. g.Value.Items]))];
    }

    private static string StableIdFor(string channel, string url)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{channel.Trim().ToLowerInvariant()}|{url.Trim()}"));
        return Convert.ToHexStringLower(hash)[..32];
    }
}
