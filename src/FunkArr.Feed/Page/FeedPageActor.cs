using Akka.Actor;
using FunkArr.Core.Actors;
using FunkArr.Messages.Mediathek;
using Terp.Akka;

namespace FunkArr.Feed.Page;

public sealed class FeedPageActor : ReceiveActor
{
    private static readonly TimeSpan _mediathekTimeout = TimeSpan.FromSeconds(30);

    private sealed record PageFetched(QueryMediathekCompleted Result, FetchPage Request);
    private sealed record PageFetchFailed(string Reason, FetchPage Request);

    private readonly IActorRef _mediathek = Context.GetActor<IMediathekManager>();

    public FeedPageActor()
    {
        Receive<FetchPage>(HandleFetchPage);
        Receive<PageFetched>(HandlePageFetched);
        Receive<PageFetchFailed>(HandlePageFetchFailed);
    }

    private void HandleFetchPage(FetchPage msg)
    {
        var replyTo = Sender;

        _mediathek.Ask<QueryMediathekResponse>(
                new QueryMediathek([], "filmlisteTimestamp", "desc", Future: true,
                    msg.Offset, msg.PageSize, 300, null),
                _mediathekTimeout)
            .PipeTo(Self,
                success: response => response switch
                {
                    QueryMediathekCompleted c => new PageFetched(c, msg),
                    QueryMediathekError e => new PageFetchFailed(
                        $"MediathekViewWeb query failed: {e.Cause.Message}", msg),
                    _ => new PageFetchFailed("MediathekViewWeb queue full", msg)
                },
                failure: ex => new PageFetchFailed(ex.Message, msg),
                sender: replyTo);
    }

    private void HandlePageFetched(PageFetched msg)
    {
        var request = msg.Request;
        var window = request.Window;
        var completed = msg.Result;

        var items = new List<MediathekItem>(completed.Items.Length);
        var reachedEnd = completed.Items.Length < request.PageSize;

        foreach (var item in completed.Items)
        {
            if (item.FilmlisteTimestamp <= window.LowerBound)
            {
                reachedEnd = true;
                break;
            }

            if (item.FilmlisteTimestamp <= window.UpperBound)
            {
                items.Add(item);
            }
        }

        Sender.Tell(new PageResult(request.Offset, [.. items], reachedEnd, null));
    }

    private void HandlePageFetchFailed(PageFetchFailed msg)
    {
        Sender.Tell(new PageResult(msg.Request.Offset, [], true, msg.Reason));
    }
}
