using Akka.Actor;
using FunkArr.Core;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;
using Servus.Akka;

namespace FunkArr.Search;

public sealed class TvSearchWorker : ReceiveActor
{
    private sealed record SearchContext(
        Guid SearchId,
        MediathekItem[] RawItems,
        string? RuleSetId);

    private SearchContext? _context;

    public TvSearchWorker()
    {
        var mediathekManager = Context.GetActor<IMediathekManager>();
        var matchMagicManager = Context.GetActor<IMatchMagicManager>();
        var ruleSetResolver = Context.GetActor<IRuleSetResolver>();

        Receive<TvSearchCommand>(cmd =>
        {
            _context = new SearchContext(cmd.SearchId, [], null);

            var fields = new List<MediathekQueryField>();
            if (!string.IsNullOrWhiteSpace(cmd.Query))
            {
                fields.Add(new MediathekQueryField(["topic"], cmd.Query));
            }

            var query = new MediathekQuery(
                Fields: fields.ToArray(),
                SortBy: "timestamp",
                SortOrder: "desc",
                Future: false,
                Offset: cmd.Offset ?? 0,
                Size: cmd.Limit ?? 50,
                DurationMin: 300,
                DurationMax: null);

            mediathekManager.Ask<object>(query, TimeSpan.FromSeconds(15))
                .PipeTo(Self, Sender);
        });

        Receive<MediathekQueryCompleted>(result =>
        {
            if (_context is null)
            {
                return;
            }

            _context = _context with { RawItems = result.Items };

            var topic = result.Items.Length > 0 ? result.Items[0].Topic : null;
            if (topic is not null)
            {
                ruleSetResolver.Ask<object>(new ResolveRuleSet(topic), TimeSpan.FromSeconds(5))
                    .PipeTo(Self, Sender);
            }
            else
            {
                ReplyWithUnscored();
            }
        });

        Receive<RuleSetResolved>(resolved =>
        {
            if (_context is null)
            {
                return;
            }

            _context = _context with { RuleSetId = resolved.RuleSetId };

            var candidates = _context.RawItems.Select(item => new ScoreCandidate(
                item.Title, item.Topic, item.Channel, item.Duration, ResolveQuality(item),
                item.Description, item.Timestamp)).ToArray();

            var requestId = Guid.NewGuid();
            var origin = new ScoringOrigin("sonarr", _context.RawItems[0].Topic);
            matchMagicManager.Ask<object>(new ScoreItems(requestId, resolved.RuleSetId, origin, candidates), TimeSpan.FromSeconds(10))
                .PipeTo(Self, Sender);
        });

        Receive<RuleSetNotFound>(_ =>
        {
            ReplyWithUnscored();
        });

        Receive<ScoreCompleted>(scored =>
        {
            if (_context is null)
            {
                return;
            }

            var items = scored.Results
                .Select(s =>
                {
                    var raw = _context.RawItems[s.Index];
                    return new SearchResultItem(
                        Title: raw.Title,
                        Channel: raw.Channel,
                        Topic: raw.Topic,
                        Url: raw.UrlVideoHd ?? raw.UrlVideo ?? raw.UrlVideoLow ?? "",
                        Duration: raw.Duration,
                        Size: raw.Size,
                        Quality: ResolveQuality(raw),
                        AiredAt: raw.Timestamp > 0
                            ? DateTimeOffset.FromUnixTimeSeconds(raw.Timestamp)
                            : null,
                        Score: s.Score);
                })
                .OrderByDescending(i => i.Score)
                .ToArray();

            Sender.Tell(new SearchCompleted(_context.SearchId, items, items.Length));
            Context.Stop(Self);
        });

        Receive<MediathekQueryFailed>(failed =>
        {
            if (_context is null)
            {
                return;
            }

            Sender.Tell(new SearchFailed(_context.SearchId, failed.Reason));
            Context.Stop(Self);
        });

        Receive<Status.Failure>(failure =>
        {
            if (_context is null)
            {
                return;
            }

            Sender.Tell(new SearchFailed(_context.SearchId, failure.Cause.Message));
            Context.Stop(Self);
        });
    }

    private void ReplyWithUnscored()
    {
        if (_context is null)
        {
            return;
        }

        var items = _context.RawItems
            .Select(raw => new SearchResultItem(
                Title: raw.Title,
                Channel: raw.Channel,
                Topic: raw.Topic,
                Url: raw.UrlVideoHd ?? raw.UrlVideo ?? raw.UrlVideoLow ?? "",
                Duration: raw.Duration,
                Size: raw.Size,
                Quality: ResolveQuality(raw),
                AiredAt: raw.Timestamp > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(raw.Timestamp)
                    : null,
                Score: 0.0))
            .ToArray();

        Sender.Tell(new SearchCompleted(_context.SearchId, items, items.Length));
        Context.Stop(Self);
    }

    private static int ResolveQuality(MediathekItem item) =>
        item.UrlVideoHd is not null ? 720 :
        item.UrlVideo is not null ? 480 :
        item.UrlVideoLow is not null ? 270 : 0;
}
