using System.Diagnostics.CodeAnalysis;
using Akka.Actor;
using FunkArr.Messages;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.History;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using FunkArr.Messages.Search;

namespace FunkArr.Search;

public sealed class TvSearchWorkerState
{
    public Guid SearchId { get; private set; }
    public IActorRef ReplyTo { get; private set; } = ActorRefs.Nobody;
    public SearchSource Source { get; private set; }
    public string? Query { get; private set; }
    public SourceInfo[] Sources { get; private set; } = [];
    public string? RuleSetId { get; private set; }
    public int? TvdbId { get; private set; }
    public string? ImdbId { get; private set; }
    public int? Season { get; private set; }
    public int? Limit { get; private set; }
    public int? Offset { get; private set; }
    public string? MediaName { get; private set; }
    public EnrichmentConfig? EnrichmentConfig { get; private set; }
    public EnrichedItem[] Items { get; private set; } = [];
    public ItemTrace[] ItemTraces { get; private set; } = [];

    private Guid _scoringRequestId;
    private ScoringOrigin? _scoringOrigin;
    private Dictionary<int, string?> _constructedTitles = [];

    public MediaIdentity BaseIdentity => new(TvdbId, ImdbId, null, null, null);

    public void Init(SearchSeries cmd, IActorRef replyTo)
    {
        SearchId = cmd.SearchId;
        ReplyTo = replyTo;
        Source = cmd.Source;
        Query = cmd.Query;
        TvdbId = cmd.TvdbId;
        ImdbId = cmd.ImdbId;
        Season = cmd.Season;
        Limit = cmd.Limit;
        Offset = cmd.Offset;
    }

    public void Apply(QueryMediathekCompleted result)
    {
        Sources = [.. result.Items.Select(SourceInfo.From)];
    }

    public void ApplyRuleSet(string ruleSetId, string? mediaName, EnrichmentConfig? enrichmentConfig)
    {
        RuleSetId = ruleSetId;
        MediaName = mediaName;
        EnrichmentConfig = enrichmentConfig;
    }

    public void Apply(ScoreCompleted scored)
    {
        var effectiveMediaName = MediaName ?? (Sources.Length > 0 ? Sources[0].Topic : "");

        _constructedTitles.Clear();
        foreach (var s in scored.Results)
        {
            if (s.Metadata?.ConstructedTitle is not null)
            {
                _constructedTitles[s.Index] = s.Metadata.ConstructedTitle;
            }
        }

        Items =
        [
            .. scored.Results.Select(s =>
            {
                var episodeTitle = _constructedTitles.GetValueOrDefault(s.Index)
                                   ?? ReleaseVariant.CleanTitle(Sources[s.Index].Title, effectiveMediaName);
                return new EnrichedItem(
                    s.Index,
                    Sources[s.Index],
                    s.Score,
                    s.Matched,
                    s.Metadata is not null,
                    BaseIdentity with
                    {
                        Season = s.Metadata?.Season,
                        Episode = s.Metadata?.Episode,
                    },
                    Match: null,
                    Display: s.Matched ? new ReleaseDisplay(effectiveMediaName, episodeTitle) : null);
            })
        ];

        ItemTraces = scored.ItemTraces;
    }

    public void Apply(EnrichEpisodesCompleted enriched)
    {
        var lookup = enriched.Episodes.ToDictionary(e => e.Index);

        Items =
        [
            .. Items.Select(item =>
            {
                if (!lookup.TryGetValue(item.Index, out var ep))
                {
                    return item;
                }

                var display = item.Display;
                if (display is not null && ep.Confidence >= 0.9f && !string.IsNullOrEmpty(ep.EpisodeName))
                {
                    display = display with { EpisodeTitle = ep.EpisodeName };
                }

                return item with
                {
                    Identity = item.Identity with { Season = ep.Season, Episode = ep.Episode },
                    Match = new MatchInfo(ep.Confidence, ep.Method),
                    Display = display,
                };
            })
        ];
    }

    public bool TryGetMediathekQuery([NotNullWhen(true)] out QueryMediathek? query)
    {
        var fields = new List<MediathekQueryField>();
        if (!string.IsNullOrWhiteSpace(Query))
        {
            fields.Add(new MediathekQueryField(["topic"], Query));
        }

        query = new QueryMediathek(
            Fields: [.. fields],
            SortBy: "timestamp",
            SortOrder: "desc",
            Future: false,
            Offset: Offset ?? 0,
            Size: Limit ?? 50,
            DurationMin: 300,
            DurationMax: null);
        return true;
    }

    public bool TryGetMediathekQueryForTopic(string topic, [NotNullWhen(true)] out QueryMediathek? query)
    {
        query = new QueryMediathek(
            Fields: [new MediathekQueryField(["topic"], topic)],
            SortBy: "timestamp",
            SortOrder: "desc",
            Future: false,
            Offset: 0,
            Size: Limit ?? 50,
            DurationMin: 300,
            DurationMax: null);
        return true;
    }

    public bool TryGetRuleSetRequest([NotNullWhen(true)] out ResolveRuleSet? request)
    {
        request = null;

        if (RuleSetId is not null)
        {
            return false;
        }

        if (Sources.Length > 0)
        {
            request = new ResolveRuleSet(Sources[0].Topic);
            return true;
        }

        if (TvdbId is not null || ImdbId is not null)
        {
            request = new ResolveRuleSet(null, TvdbId, ImdbId);
            return true;
        }

        return false;
    }

    public bool TryGetScoringRequest([NotNullWhen(true)] out ScoreItems? request)
    {
        request = null;

        if (Sources.Length == 0 || RuleSetId is null)
        {
            return false;
        }

        var candidates = Sources.Select(s => new ScoreCandidate(
            s.Title, s.Topic, s.Channel, s.Duration, s.ResolveQuality(),
            s.Description, s.AiredAt?.ToUnixTimeSeconds() ?? 0)).ToArray();

        _scoringRequestId = Guid.NewGuid();
        _scoringOrigin = new ScoringOrigin(Source, Sources[0].Topic);
        request = new ScoreItems(_scoringRequestId, RuleSetId, _scoringOrigin, candidates);
        return true;
    }

    public bool TryGetEnrichmentRequest([NotNullWhen(true)] out EnrichEpisodes? request)
    {
        request = null;

        if (TvdbId is null)
        {
            return false;
        }

        if (EnrichmentConfig is { Enabled: false })
        {
            return false;
        }

        var candidates = Items
            .Where(e => e.Matched && e.HasScoringMetadata && e.Match is null)
            .Select(e => new EpisodeCandidate(
                e.Index, e.Source.Title,
                _constructedTitles.GetValueOrDefault(e.Index),
                e.Source.AiredAt, e.Source.Duration,
                e.Identity.Season, e.Identity.Episode))
            .ToArray();

        if (candidates.Length == 0)
        {
            return false;
        }

        request = new EnrichEpisodes(TvdbId.Value, Season, candidates, EnrichmentConfig);
        return true;
    }

    public SearchSeriesCompleted GetSnapshot() => ToSearchCompleted();

    public SearchSeriesCompleted ToSearchCompleted()
    {
        var items = Items.Length > 0 ? EnsureDisplay(Items) : UnscoredItems();

        var variants = items
            .SelectMany(e => ReleaseVariant.Expand(e, MediaType.Show, MediaName))
            .OrderByDescending(v => v.Score)
            .Select(v => v.ToResultItem())
            .ToArray();

        return new SearchSeriesCompleted(SearchId, variants, variants.Length);
    }

    public void MergeEnrichmentIntoTraces(EnrichedEpisode[] enriched)
    {
        var lookup = enriched.ToDictionary(e => e.Index);
        ItemTraces =
        [
            .. ItemTraces.Select(trace =>
            {
                if (!trace.Matched)
                {
                    return trace;
                }

                var candidateIndex = Array.FindIndex(Items, i => i.Source.Title == trace.CandidateTitle && i.Matched);
                if (candidateIndex < 0)
                {
                    return trace;
                }

                if (lookup.TryGetValue(candidateIndex, out var ep))
                {
                    return trace with
                    {
                        EnrichmentTrace = new EnrichmentTrace(
                            ep.Method, ep.Confidence, true,
                            ep.Season, ep.Episode, ep.EpisodeName)
                    };
                }

                return trace with
                {
                    EnrichmentTrace = new EnrichmentTrace(
                        MatchMethod.TitleMatch, 0f, false,
                        Detail: "no matching episode found")
                };
            })
        ];
    }

    public RecordHistory? BuildRecordHistory()
    {
        if (RuleSetId is null || _scoringOrigin is null || ItemTraces.Length == 0)
        {
            return null;
        }

        var matchedCount = Items.Count(i => i.Matched);
        var enrichedCount = ItemTraces.Count(t => t.EnrichmentTrace is { Enriched: true });

        return new RecordHistory(
            _scoringRequestId, RuleSetId, _scoringOrigin,
            DateTimeOffset.UtcNow,
            Sources.Length, matchedCount, enrichedCount,
            ItemTraces);
    }

    private EnrichedItem[] EnsureDisplay(EnrichedItem[] items)
    {
        var fallbackMediaName = MediaName ?? (Sources.Length > 0 ? Sources[0].Topic : "");
        return
        [
            .. items.Select(item =>
                item.Display is not null
                    ? item
                    : item with
                    {
                        Display = new ReleaseDisplay(
                            fallbackMediaName,
                            ReleaseVariant.CleanTitle(item.Source.Title, fallbackMediaName)),
                    })
        ];
    }

    private EnrichedItem[] UnscoredItems()
    {
        var fallbackMediaName = MediaName ?? (Sources.Length > 0 ? Sources[0].Topic : "");
        return
        [
            .. Sources.Select((s, i) => new EnrichedItem(
                i, s, 0.0, false, false, BaseIdentity, null,
                Display: new ReleaseDisplay(
                    fallbackMediaName,
                    ReleaseVariant.CleanTitle(s.Title, fallbackMediaName))))
        ];
    }
}
