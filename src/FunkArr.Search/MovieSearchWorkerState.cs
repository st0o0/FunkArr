using System.Diagnostics.CodeAnalysis;
using Akka.Actor;
using FunkArr.Messages;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;

namespace FunkArr.Search;

public sealed class MovieSearchWorkerState
{
    public Guid SearchId { get; private set; }
    public IActorRef ReplyTo { get; private set; } = ActorRefs.Nobody;
    public SearchSource Source { get; private set; }
    public string? Query { get; private set; }
    public SourceInfo[] Sources { get; private set; } = [];
    public string? RuleSetId { get; private set; }
    public string? ImdbId { get; private set; }
    public int? TmdbId { get; private set; }
    public int? Limit { get; private set; }
    public int? Offset { get; private set; }
    public string? MediaName { get; private set; }
    public EnrichmentConfig? EnrichmentConfig { get; private set; }
    public EnrichedItem[] Items { get; private set; } = [];

    public MediaIdentity BaseIdentity => new(null, ImdbId, TmdbId, null, null);

    public void Init(SearchMovie cmd, IActorRef replyTo)
    {
        SearchId = cmd.SearchId;
        ReplyTo = replyTo;
        Source = cmd.Source;
        Query = cmd.Query;
        ImdbId = cmd.ImdbId;
        TmdbId = cmd.TmdbId;
        Limit = cmd.Limit;
        Offset = cmd.Offset;
    }

    public void Apply(QueryMediathekCompleted result)
    {
        Sources = result.Items.Select(SourceInfo.From).ToArray();
    }

    public void ApplyRuleSet(string ruleSetId, string? mediaName, EnrichmentConfig? enrichmentConfig)
    {
        RuleSetId = ruleSetId;
        MediaName = mediaName;
        EnrichmentConfig = enrichmentConfig;
    }

    public void Apply(ScoreCompleted scored)
    {
        Items = scored.Results.Select(s => new EnrichedItem(
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
            Match: null)).ToArray();
    }

    public void Apply(EnrichMoviesCompleted enriched)
    {
        var lookup = enriched.Movies.ToDictionary(m => m.Index);

        Items = Items.Select(item =>
            lookup.TryGetValue(item.Index, out var movie)
                ? item with
                {
                    Identity = item.Identity with
                    {
                        ImdbId = movie.ImdbId ?? item.Identity.ImdbId,
                        TmdbId = movie.TmdbId ?? item.Identity.TmdbId,
                    },
                    Match = new MatchInfo(movie.Confidence, movie.Method),
                }
                : item).ToArray();
    }

    public bool TryGetMediathekQuery([NotNullWhen(true)] out QueryMediathek? query)
    {
        var fields = new List<MediathekQueryField>();
        if (!string.IsNullOrWhiteSpace(Query))
        {
            fields.Add(new MediathekQueryField(["title", "topic"], Query));
        }

        query = new QueryMediathek(
            Fields: fields.ToArray(),
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
            request = new ResolveRuleSet(null, ImdbId: ImdbId, TmdbId: TmdbId);
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

        var origin = new ScoringOrigin(Source, Sources[0].Topic);
        request = new ScoreItems(Guid.NewGuid(), RuleSetId, origin, candidates);
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

    public SearchMovieCompleted ToSearchCompleted()
    {
        var items = Items.Length > 0 ? Items : UnscoredItems();

        var variants = items
            .SelectMany(e => ReleaseVariant.Expand(e, MediaType.Movie, MediaName))
            .OrderByDescending(v => v.Score)
            .Select(v => v.ToResultItem())
            .ToArray();

        return new SearchMovieCompleted(SearchId, variants, variants.Length);
    }

    private EnrichedItem[] UnscoredItems() =>
        Sources.Select((s, i) => new EnrichedItem(i, s, 0.0, false, false, BaseIdentity, null)).ToArray();
}
