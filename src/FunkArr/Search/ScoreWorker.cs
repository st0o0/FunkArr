using Akka.Actor;
using FunkArr.Shared.Models;

namespace FunkArr.Search;

internal sealed class ScoreWorker : ReceiveActor
{
    public ScoreWorker()
    {
        Receive<ScoreResults>(Handle);
    }

    private void Handle(ScoreResults message)
    {
        var scored = message.Results
            .Select(r => MatchingPipeline.ScoreResult(r, message.Context))
            .OrderByDescending(r => r.Score)
            .ToList();

        Sender.Tell(new ResultsScored(scored));
    }
}
