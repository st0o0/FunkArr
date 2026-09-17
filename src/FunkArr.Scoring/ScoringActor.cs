using Akka.Actor;
using FunkArr.Core;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using Servus.Akka;

namespace FunkArr.Scoring;

public sealed class ScoringActor : ReceiveActor
{
    private readonly IActorRef _historyRegion = Context.GetActor<IScoringHistoryRegion>();

    public ScoringActor()
    {
        Receive<ExecuteScoring>(Handle);
    }

    private void Handle(ExecuteScoring msg)
    {
        var (scored, itemTraces) = ScoringEngine.Score(msg.Config, msg.Items);

        if (msg.Origin.Source == "Test")
        {
            Sender.Tell(new TestScoreCompleted(msg.RequestId, itemTraces));
        }
        else
        {
            Sender.Tell(new ScoreCompleted(msg.RequestId, scored));

            var matchedCount = scored.Count(s => s.Matched);
            _historyRegion.Tell(new RecordScoring(
                msg.RequestId, msg.Config.RuleSetId, msg.Origin,
                DateTimeOffset.UtcNow, msg.Items.Length, matchedCount, itemTraces));
        }
    }
}
