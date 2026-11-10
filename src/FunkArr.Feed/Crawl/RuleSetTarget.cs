using FunkArr.Messages;
using FunkArr.Messages.Shared;

namespace FunkArr.Feed.Crawl;

internal sealed record RuleSetTarget(string RuleSetId, MediaType MediaType, ExternalIds Ids);
