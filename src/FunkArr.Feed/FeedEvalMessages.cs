using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Shared;

namespace FunkArr.Feed;

internal sealed record EvalGroup(
    string RuleSetId,
    MediaType MediaType,
    ExternalIds Ids,
    MediathekItem[] Items,
    double MinConfidence);

internal sealed record GroupResult(
    string RuleSetId,
    FeedEntry[] Accepted,
    IReadOnlyDictionary<string, int>? RejectedByReason,
    string? Error);
