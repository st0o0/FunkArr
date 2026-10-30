using System.Diagnostics.CodeAnalysis;
using Akka.Actor;
using FunkArr.Messages;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Search;
using FunkArr.Messages.Shared;

namespace FunkArr.Search;

public sealed class MovieSearchWorkerState
{
    private const int _defaultSize = 50;
    private const int _durationMin = 3600;

    public Guid SearchId { get; }
    public IActorRef ReplyTo { get; private set; } = ActorRefs.Nobody;
    public SearchSource Source { get; private set; }
    public string? Query { get; private set; }
    public string? ImdbId { get; private set; }
    public int? TmdbId { get; private set; }
    public int? Limit { get; private set; }
    public int? Offset { get; private set; }
    public int Attempt { get; set; }
    public RuleSetResolved? RuleSet { get; private set; }
    public MediathekItem[] Items { get; private set; } = [];

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

    public bool TryGetRuleSetRequest([NotNullWhen(true)] out ResolveRuleSet? request)
    {
        request = RuleSet is null && (ImdbId is not null || TmdbId is not null)
            ? new ResolveRuleSet(null, new ExternalIds(null, ImdbId, TmdbId))
            : null;
        return request is not null;
    }

    public void ApplyRuleSet(RuleSetResolved resolved) => RuleSet = resolved;

    public QueryMediathek[] GetMediathekQueries()
    {
        var topic = RuleSet?.Topic ?? Query;
        MediathekQueryField[] fields = string.IsNullOrWhiteSpace(topic) ? [] : [new MediathekQueryField(["title", "topic"], topic)];

        var primary = new QueryMediathek(
            Fields: fields,
            SortBy: "timestamp",
            SortOrder: "desc",
            Future: false,
            Offset: RuleSet is null ? Offset ?? 0 : 0,
            Size: Limit ?? _defaultSize,
            DurationMin: _durationMin,
            DurationMax: null);

        return [primary, primary with { Future = true }];
    }

    public void Apply(QueryMediathekCompleted result) => Items = result.Items;

    public EvaluateMovies ToEvaluation(Guid evaluationId) =>
        new(evaluationId, Source, Items, ImdbId, TmdbId, RuleSet);

    public SearchMovieCompleted ToSearchCompleted(EvaluateMoviesCompleted evaluation) =>
        new(SearchId, evaluation.Items, evaluation.Items.Length);

    public SearchMovieCompleted Empty() => new(SearchId, [], 0);
}
