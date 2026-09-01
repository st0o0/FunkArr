using Akka.Actor;
using FunkArr.Messages.Scoring;

namespace FunkArr.MatchMagic;

internal sealed record ExecuteScoring(
    MatchingConfig Config,
    ScoreCandidate[] Items,
    Guid RequestId,
    ScoringOrigin Origin,
    IActorRef HistoryRef);
