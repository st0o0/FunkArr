using FunkArr.Api.Contracts;
using FunkArr.RuleSet;

namespace FunkArr.Api.Mapping;

public static class ContractMappingExtensions
{
    public static MatchedTraceContract ToContract(this MatchedTrace t) =>
        new(t.Confidence, t.Episode, t.EpisodeName, t.ItemChannel, t.ItemDuration,
            t.ItemTitle, t.ItemTopic, t.RuleIndex, t.Season,
            Enum.Parse<MatchedTraceContractStrategy>(t.Strategy.ToString()));

    public static FilteredTraceContract ToContract(this FilteredTrace t) =>
        new(t.ActualValue, t.FilterField, t.FilterOp, t.FilterValue,
            t.ItemChannel, t.ItemDuration, t.ItemTitle, t.ItemTopic, t.Reason);

    public static UnmatchedTraceContract ToContract(this UnmatchedTrace t) =>
        new(t.ItemChannel, t.ItemDuration, t.ItemTitle, t.ItemTopic,
            t.RuleFailures.Select(f => f.ToContract()).ToList());

    public static RuleFailureContract ToContract(this RuleFailure f) =>
        new(f.Detail, f.FailReason, f.RuleIndex);

    public static MatchSummary ToContract(this MatchRecord r) =>
        new(r.Episode ?? 0, r.Filtered.Select(f => f.ToContract()).ToList(),
            r.Id, r.Matched.Select(m => m.ToContract()).ToList(),
            r.SearchTopic, r.Season ?? 0, r.Source, r.Timestamp,
            r.TotalResults, r.TvdbId ?? 0,
            r.Unmatched.Select(u => u.ToContract()).ToList());

    public static TopicSummary ToContract(this TopicStats s) =>
        new(s.FilteredCount, s.MatchedCount, s.MatchRate,
            s.PerRuleHitCounts, s.SearchCount, s.Topic,
            s.TotalItemsEvaluated, s.UnmatchedCount);

    public static Contracts.UnmatchedGroup ToContract(this MatchQualityActor.UnmatchedGroup g) =>
        new(g.Items.Select(i => i.ToContract()).ToList(), g.Topic);

    public static RulesetSummary ToContract(
        this RuleSetActor.RuleSetSummary rs,
        TopicStats? stats) =>
        new(rs.Aliases?.ToList(),
            stats?.MatchRate ?? 0,
            rs.Media is not null ? rs.Media.ToContract() : null,
            rs.RuleCount, stats?.SearchCount ?? 0, rs.Source, rs.Topic);

    public static Contracts.MediaReference ToContract(this RuleSet.MediaReference m) =>
        new(m.ImdbId, m.Name, m.TmdbId ?? 0, m.TvdbId ?? 0, m.Type);

    public static TestRulesResult ToContract(this RuleSetActor.TestRulesResponse r) =>
        new(r.Filtered.Select(f => f.ToContract()).ToList(),
            r.Matched.Select(m => m.ToContract()).ToList(),
            r.TotalItems,
            r.Unmatched.Select(u => u.ToContract()).ToList());
}
