using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Shared;

namespace FunkArr.Feed.Eval;

internal sealed record EvalGroup(
    string RuleSetId,
    MediaType MediaType,
    ExternalIds Ids,
    MediathekItem[] Items,
    double MinConfidence);

internal sealed record GroupResult(
    string RuleSetId,
    FeedEntry[] Accepted,
    IReadOnlyDictionary<RejectionReason, int>? RejectedByReason,
    string? Error);
