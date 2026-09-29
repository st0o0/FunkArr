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
using FunkArr.Messages.Shared;

namespace FunkArr.Search;

public sealed class MovieSearchWorkerState
{
    private Guid _scoringRequestId;
    private ScoringOrigin? _scoringOrigin;

    private MovieIdentity BaseIdentity => new(ImdbId, TmdbId, null);

    public Guid SearchId { get; private set; }
    public IActorRef ReplyTo { get; private set; } = ActorRefs.Nobody;
    public SearchSource Source { get; private set; }
    public string? Query { get; private set; }
    public SourceInfo[] Sources { get; private set; } = [];
    public string? RuleSetId { get; set; }
    public string? ImdbId { get; private set; }
    public int? TmdbId { get; private set; }
    public int? Limit { get; private set; }
    public int? Offset { get; private set; }
    public string? MediaName { get; set; }
    public EnrichmentConfig? EnrichmentConfig { get; private set; }
    public int Attempt { get; set; }
    public EnrichedItem[] Items { get; private set; } = [];
    public ItemTrace[] ItemTraces { get; set; } = [];

    private MovieSearchWorkerState(string nttId)
    {
        SearchId = Guid.Parse(nttId);
    }

    public static MovieSearchWorkerState From(string nttId) => new(nttId);

    public void Apply(SearchMovie cmd, IActorRef replyTo)
    {
        ReplyTo = replyTo;
        Source = cmd.Source;
        Query = cmd.Query;
        ImdbId = cmd.ImdbId;
        TmdbId = cmd.TmdbId;
        Limit = cmd.Limit;
        Offset = cmd.Offset;
    }

    public void Apply(QueryMediathekCompleted result) => Sources = [.. result.Items.Select(SourceInfo.From)];

    public void Apply(string ruleSetId, string? mediaName, EnrichmentConfig? enrichmentConfig)
    {
        RuleSetId = ruleSetId;
        MediaName = mediaName;
        EnrichmentConfig = enrichmentConfig;
    }

    public void Apply(ScoreCompleted scored)
    {
        var effectiveMediaName = MediaName ?? (Sources.Length > 0 ? Sources[0].Topic : "");

        Items =
        [
            .. scored.Results.Select(s =>
            {
                var baseDisplay = ReleaseDisplay.From(Sources[s.Index].Title, effectiveMediaName);
                var constructedTitle = s.Metadata?.ConstructedTitle;
                var display = constructedTitle is not null
                    ? baseDisplay with { EpisodeTitle = constructedTitle }
                    : baseDisplay;

                return new EnrichedItem(
                    s.Index,
                    Sources[s.Index],
                    s.Score,
                    s.Matched,
                    s.Metadata is not null,
                    BaseIdentity,
                    Match: null,
                    Display: display);
            })
        ];

        ItemTraces = scored.ItemTraces;
    }

    public void Apply(EnrichMoviesCompleted enriched)
    {
        var lookup = enriched.Movies.ToDictionary(m => m.Index);

        Items =
        [
            .. Items.Select(item =>
            {
                if (!lookup.TryGetValue(item.Index, out var movie))
                {
                    return item;
                }

                var display = item.Display;
                if (display is not null && movie.Confidence >= 0.9f && !string.IsNullOrEmpty(movie.Title))
                {
                    display = display with { EpisodeTitle = movie.Title };
                }

                var mi = (MovieIdentity)item.Identity;
                return item with
                {
                    Identity = mi with
                    {
                        ImdbId = movie.ImdbId ?? mi.ImdbId,
                        TmdbId = movie.TmdbId ?? mi.TmdbId,
                        Year = movie.Year
                    },
                    Match = new MatchInfo(movie.Confidence, movie.Method),
                    Display = display
                };
            })
        ];
    }

    public bool TryGetMediathekQuery([NotNullWhen(true)] out QueryMediathek? query)
    {
        var fields = new List<MediathekQueryField>();
        if (!string.IsNullOrWhiteSpace(Query))
        {
            fields.Add(new MediathekQueryField(["title", "topic"], Query));
        }

        query = new QueryMediathek(
            Fields: [.. fields],
            SortBy: "timestamp",
            SortOrder: "desc",
            Future: false,
            Offset: Offset ?? 0,
            Size: Limit ?? 50,
            DurationMin: 3600,
            DurationMax: null);
        return true;
    }

    public bool TryGetMediathekQueryForTopic(string topic, [NotNullWhen(true)] out QueryMediathek? query)
    {
        query = new QueryMediathek(
            Fields: [new MediathekQueryField(["title", "topic"], topic)],
            SortBy: "timestamp",
            SortOrder: "desc",
            Future: false,
            Offset: 0,
            Size: Limit ?? 50,
            DurationMin: 3600,
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

        if (ImdbId is not null || TmdbId is not null)
        {
            request = new ResolveRuleSet(null, new ExternalIds(null, ImdbId, TmdbId));
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

    public bool TryGetEnrichmentRequest([NotNullWhen(true)] out EnrichMovies? request)
    {
        request = null;

        if (ImdbId is null && TmdbId is null)
        {
            return false;
        }

        if (EnrichmentConfig is { Enabled: false })
        {
            return false;
        }

        var candidates = Items
            .Where(e => e.Matched)
            .Select(e => new MovieCandidate(
                e.Index, e.Source.Title,
                e.Source.AiredAt, e.Source.Duration))
            .ToArray();

        if (candidates.Length == 0)
        {
            return false;
        }

        request = new EnrichMovies(ImdbId, TmdbId, candidates, EnrichmentConfig);
        return true;
    }

    public SearchMovieCompleted GetSnapshot() => ToSearchCompleted();

    public SearchMovieCompleted ToSearchCompleted()
    {
        var items = Items.Length > 0 ? Items : UnscoredItems();

        var results = items
            .SelectMany(e => SceneRelease.ForMovie(e, MediaName).Expand())
            .OrderByDescending(r => r.Score)
            .ToArray();

        return new SearchMovieCompleted(SearchId, results, results.Length);
    }

    public void MergeEnrichmentIntoTraces(EnrichedMovie[] enriched)
    {
        var lookup = enriched.ToDictionary(m => m.Index);
        ItemTraces =
        [
            .. ItemTraces.Select(trace =>
            {
                if (!trace.Matched)
                {
                    return trace;
                }

                var candidateIndex = Array.FindIndex(Items, i => i.Source.Title == trace.Candidate.Title && i.Matched);
                if (candidateIndex < 0)
                {
                    return trace;
                }

                if (lookup.TryGetValue(candidateIndex, out var movie))
                {
                    return trace with
                    {
                        EnrichmentTrace = new EnrichmentTrace(
                            movie.Method, movie.Confidence, true,
                            ResolvedTitle: movie.Title, ResolvedYear: movie.Year)
                    };
                }

                return trace with
                {
                    EnrichmentTrace = new EnrichmentTrace(
                        MatchMethod.TitleMatch, 0f, false,
                        Detail: "no matching movie found")
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

    private EnrichedItem[] UnscoredItems()
    {
        var fallbackMediaName = MediaName ?? (Sources.Length > 0 ? Sources[0].Topic : "");
        return
        [
            .. Sources.Select((s, i) => new EnrichedItem(
                i, s, 0.0, false, false, BaseIdentity, null,
                Display: ReleaseDisplay.From(s.Title, fallbackMediaName)))
        ];
    }
}
