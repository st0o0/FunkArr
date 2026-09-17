namespace FunkArr.Messages.RuleSet;

public sealed class RuleSetNotFoundException(string topicOrAlias)
    : Exception($"RuleSet not found for '{topicOrAlias}'")
{
    public string TopicOrAlias { get; } = topicOrAlias;
}
