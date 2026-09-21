using Akka.Actor;
using FunkArr.Messages;
using FunkArr.Messages.Scoring;

namespace FunkArr.Scoring;

public sealed class ScoringActor : ReceiveActor
{
    public ScoringActor()
    {
        Receive<ExecuteScoring>(Handle);
    }

    private void Handle(ExecuteScoring msg)
    {
        var (scored, itemTraces) = ScoringEngine.Score(msg.Config, msg.Items);

        if (msg.Origin.Source == SearchSource.Test)
        {
            Sender.Tell(new TestScoreCompleted(msg.RequestId, itemTraces));
        }
        else
        {
            Sender.Tell(new ScoreCompleted(msg.RequestId, scored, itemTraces));
        }
    }
}
