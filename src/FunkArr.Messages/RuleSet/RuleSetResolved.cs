namespace FunkArr.Messages.RuleSet;

public sealed record RuleSetResolved(string RuleSetId, string Topic, string? MediaName = null) : IRuleSetResponse;
