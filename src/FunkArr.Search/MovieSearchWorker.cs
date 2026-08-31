using Akka.Actor;
using FunkArr.Core;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;
using Servus.Akka;

namespace FunkArr.Search;

public sealed class MovieSearchWorker : ReceiveActor
{
    private sealed record SearchContext(Guid SearchId, IActorRef ReplyTo, MediathekItem[] RawItems);

    private SearchContext? _context;

    public MovieSearchWorker()
    {
        var mediathekManager = Context.GetActor<IMediathekGateway>();
        var matchMagicManager = Context.GetActor<IMatchMagicService>();

        Receive<MovieSearchCommand>(cmd =>
        {
            _context = new SearchContext(cmd.SearchId, Sender, []);

            var fields = new List<MediathekQueryField>();
            if (!string.IsNullOrWhiteSpace(cmd.Query))
                fields.Add(new MediathekQueryField(["title", "topic"], cmd.Query));

            if (fields.Count == 0)
            {
                Sender.Tell(new SearchFailed(cmd.SearchId, "Movie search requires a query"));
                Context.Stop(Self);
                return;
            }

            var query = new MediathekQuery(
                Fields: fields.ToArray(),
                SortBy: "timestamp",
                SortOrder: "desc",
                Future: false,
                Offset: 0,
                Size: 50,
                DurationMin: 3600,
                DurationMax: null);

            mediathekManager.Ask<object>(query, TimeSpan.FromSeconds(15))
                .PipeTo(Self, Sender);
        });

        Receive<MediathekQueryCompleted>(result =>
        {
            if (_context is null) return;
            _context = _context with { RawItems = result.Items };

            var candidates = result.Items.Select(item => new ScoreCandidate(
                item.Title, item.Topic, item.Channel, item.Duration, ResolveQuality(item))).ToArray();

            matchMagicManager.Ask<object>(new ScoreItems(candidates, null), TimeSpan.FromSeconds(10))
                .PipeTo(Self, Sender);
        });

        Receive<ScoreCompleted>(scored =>
        {
            if (_context is null) return;

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

            _context.ReplyTo.Tell(new SearchCompleted(_context.SearchId, items, items.Length));
            Context.Stop(Self);
        });

        Receive<MediathekQueryFailed>(failed =>
        {
            if (_context is null) return;
            _context.ReplyTo.Tell(new SearchFailed(_context.SearchId, failed.Reason));
            Context.Stop(Self);
        });

        Receive<Status.Failure>(failure =>
        {
            if (_context is null) return;
            _context.ReplyTo.Tell(new SearchFailed(_context.SearchId, failure.Cause.Message));
            Context.Stop(Self);
        });
    }

    private static int ResolveQuality(MediathekItem item) =>
        item.UrlVideoHd is not null ? 720 :
        item.UrlVideo is not null ? 480 :
        item.UrlVideoLow is not null ? 270 : 0;
}
