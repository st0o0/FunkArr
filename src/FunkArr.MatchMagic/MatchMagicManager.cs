using Akka.Actor;
using FunkArr.Messages.Scoring;

namespace FunkArr.MatchMagic;

public sealed class MatchMagicManager : ReceiveActor
{
    private sealed record State(Dictionary<string, RuleSet> RuleSets);

    private State _state = new(new Dictionary<string, RuleSet>());

    public MatchMagicManager()
    {
        Receive<LoadRuleSet>(HandleLoadRuleSet);
        Receive<UnloadRuleSet>(HandleUnloadRuleSet);
        Receive<ScoreItems>(HandleScoreItems);
    }

    private void HandleLoadRuleSet(LoadRuleSet msg)
    {
        var ruleSet = RuleSet.FromJson(msg.Json);
        _state.RuleSets[msg.Id] = ruleSet;
    }

    private void HandleUnloadRuleSet(UnloadRuleSet msg)
    {
        _state.RuleSets.Remove(msg.Id);
    }

    private void HandleScoreItems(ScoreItems msg)
    {
        var ruleSet = ResolveRuleSet(msg.RuleSetId);
        if (ruleSet is null)
        {
            var defaults = msg.Items.Select((_, i) => new ScoredItem(i, 0.0, false)).ToArray();
            Sender.Tell(new ScoreCompleted(defaults));
            return;
        }

        var mediaItems = msg.Items.Select(c => new MediaItem(
            Topic: c.Topic,
            Title: c.Title,
            Description: null,
            Channel: c.Channel,
            Timestamp: 0,
            Duration: c.Duration,
            UrlVideoHd: null,
            UrlVideo: null,
            UrlVideoLow: null)).ToArray();

        var matchResults = ruleSet.Evaluate(mediaItems);
        var matchedSet = new HashSet<MediaItem>(matchResults.Select(r => r.Item));

        var scored = msg.Items.Select((candidate, index) =>
        {
            var matchResult = matchResults.FirstOrDefault(r =>
                r.Item.Title == candidate.Title &&
                r.Item.Topic == candidate.Topic &&
                r.Item.Channel == candidate.Channel);

            return matchResult is not null
                ? new ScoredItem(index, matchResult.Confidence, true)
                : new ScoredItem(index, 0.0, false);
        }).ToArray();

        Sender.Tell(new ScoreCompleted(scored));
    }

    private RuleSet? ResolveRuleSet(string? id)
    {
        if (id is not null && _state.RuleSets.TryGetValue(id, out var specific))
            return specific;

        return _state.RuleSets.Values.FirstOrDefault();
    }
}
