using System.Diagnostics.CodeAnalysis;
using Akka.Actor;
using FunkArr.Messages;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Search;
using FunkArr.Messages.Shared;

namespace FunkArr.Search;

public sealed class TvSearchWorkerState
{
    private const int _defaultSize = 50;
    private const int _durationMin = 300;

    public Guid SearchId { get; }
    public IActorRef ReplyTo { get; private set; } = ActorRefs.Nobody;
    public SearchSource Source { get; private set; }
    public string? Query { get; private set; }
    public int? TvdbId { get; private set; }
    public string? ImdbId { get; private set; }
    public int? Season { get; private set; }
    public int? Limit { get; private set; }
    public int? Offset { get; private set; }
    public int Attempt { get; set; }
    public RuleSetResolved? RuleSet { get; private set; }
    public MediathekItem[] Items { get; private set; } = [];

    private TvSearchWorkerState(string nttId)
    {
        SearchId = Guid.Parse(nttId);
    }

    public static TvSearchWorkerState From(string nttId) => new(nttId);

    public void Init(SearchSeries cmd, IActorRef replyTo)
    {
        ReplyTo = replyTo;
        Source = cmd.Source;
        Query = cmd.Query;
        TvdbId = cmd.TvdbId;
        ImdbId = cmd.ImdbId;
        Season = cmd.Season;
        Limit = cmd.Limit;
        Offset = cmd.Offset;
    }

    public bool TryGetRuleSetRequest([NotNullWhen(true)] out ResolveRuleSet? request)
    {
        request = RuleSet is null && (TvdbId is not null || ImdbId is not null)
            ? new ResolveRuleSet(null, new ExternalIds(TvdbId, ImdbId, null))
            : null;
        return request is not null;
    }

    public void ApplyRuleSet(RuleSetResolved resolved) => RuleSet = resolved;

    public QueryMediathek GetMediathekQuery()
    {
        var topic = RuleSet?.Topic ?? Query;
        MediathekQueryField[] fields = string.IsNullOrWhiteSpace(topic) ? [] : [new MediathekQueryField(["topic"], topic)];

        return new QueryMediathek(
            Fields: fields,
            SortBy: "timestamp",
            SortOrder: "desc",
            Offset: RuleSet is null ? Offset ?? 0 : 0,
            Size: Limit ?? _defaultSize,
            DurationMin: _durationMin,
            DurationMax: null);
    }

    public void Apply(QueryMediathekCompleted result) => Items = result.Items;

    public EvaluateSeries ToEvaluation(Guid evaluationId) =>
        new(evaluationId, Source, Items, TvdbId, ImdbId, Season, RuleSet);

    public SearchSeriesCompleted ToSearchCompleted(EvaluateSeriesCompleted evaluation) =>
        new(SearchId, evaluation.Items, evaluation.Items.Length);

    public SearchSeriesCompleted Empty() => new(SearchId, [], 0);
}
