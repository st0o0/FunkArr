using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;

namespace FunkArr.Persistence.MatchHistory;

public static class ScoringRecordedMapper
{
    public static ScoringRecordedDto ToDto(RecordScoringResult msg) => new()
    {
        Version = 1,
        RequestId = msg.RequestId.ToString(),
        Source = msg.Origin.Source,
        Query = msg.Origin.Query,
        Timestamp = msg.Timestamp.ToString("O"),
        CandidateCount = msg.CandidateCount,
        MatchedCount = msg.MatchedCount,
        ItemTraces = msg.ItemTraces.Select(ItemTraceToDto).ToArray()
    };

    public static RecordScoringResult FromDto(ScoringRecordedDto dto, string ruleSetId) => new(
        RequestId: Guid.Parse(dto.RequestId),
        RuleSetId: ruleSetId,
        Origin: new ScoringOrigin(dto.Source, dto.Query),
        Timestamp: DateTimeOffset.Parse(dto.Timestamp),
        CandidateCount: dto.CandidateCount,
        MatchedCount: dto.MatchedCount,
        ItemTraces: dto.ItemTraces.Select(ItemTraceFromDto).ToArray());

    public static ItemTraceDto ItemTraceToDto(ItemTrace trace) => new()
    {
        CandidateTitle = trace.CandidateTitle,
        CandidateTopic = trace.CandidateTopic,
        CandidateChannel = trace.CandidateChannel,
        CandidateDuration = trace.CandidateDuration,
        CandidateQuality = trace.CandidateQuality,
        CandidateDescription = trace.CandidateDescription,
        CandidateTimestamp = trace.CandidateTimestamp,
        Matched = trace.Matched,
        Score = trace.Score,
        MatchedRuleId = trace.MatchedRuleId,
        Identification = trace.Identification is not null ? IdentificationToDto(trace.Identification) : null,
        RuleTraces = trace.RuleTraces.Select(RuleTraceToDto).ToArray()
    };

    public static ItemTrace ItemTraceFromDto(ItemTraceDto dto) => new(
        CandidateTitle: dto.CandidateTitle,
        CandidateTopic: dto.CandidateTopic,
        CandidateChannel: dto.CandidateChannel,
        CandidateDuration: dto.CandidateDuration,
        CandidateQuality: dto.CandidateQuality,
        CandidateDescription: dto.CandidateDescription,
        CandidateTimestamp: dto.CandidateTimestamp,
        Matched: dto.Matched,
        Score: dto.Score,
        MatchedRuleId: dto.MatchedRuleId,
        Identification: dto.Identification is not null ? IdentificationFromDto(dto.Identification) : null,
        RuleTraces: dto.RuleTraces.Select(RuleTraceFromDto).ToArray());

    private static RuleTraceDto RuleTraceToDto(RuleTrace trace) => new()
    {
        RuleId = trace.RuleId,
        Priority = trace.Priority,
        Outcome = trace.Outcome.ToString(),
        FilterTrace = trace.FilterTrace is not null ? FilterGroupToDto(trace.FilterTrace) : null,
        IdentificationTrace = trace.IdentificationTrace is not null ? IdentificationTraceToDto(trace.IdentificationTrace) : null
    };

    private static RuleTrace RuleTraceFromDto(RuleTraceDto dto) => new(
        RuleId: dto.RuleId,
        Priority: dto.Priority,
        Outcome: Enum.Parse<RuleOutcome>(dto.Outcome),
        FilterTrace: dto.FilterTrace is not null ? FilterGroupFromDto(dto.FilterTrace) : null,
        IdentificationTrace: dto.IdentificationTrace is not null ? IdentificationTraceFromDto(dto.IdentificationTrace) : null);

    private static FilterGroupTraceDto FilterGroupToDto(FilterGroupTrace trace) => new()
    {
        Operator = trace.Operator,
        Passed = trace.Passed,
        Nodes = trace.Nodes.Select(FilterNodeToDto).ToArray()
    };

    private static FilterGroupTrace FilterGroupFromDto(FilterGroupTraceDto dto) => new(
        Operator: dto.Operator,
        Passed: dto.Passed,
        Nodes: dto.Nodes.Select(FilterNodeFromDto).ToArray());

    private static FilterNodeTraceDto FilterNodeToDto(FilterNodeTrace trace) => new()
    {
        NodeType = trace.Group is not null ? "group" : "condition",
        Field = trace.Field,
        Op = trace.Op,
        ExpectedValue = trace.ExpectedValue,
        ActualValue = trace.ActualValue,
        Passed = trace.Passed,
        Skipped = trace.Skipped,
        Group = trace.Group is not null ? FilterGroupToDto(trace.Group) : null
    };

    private static FilterNodeTrace FilterNodeFromDto(FilterNodeTraceDto dto) => new(
        Field: dto.Field,
        Op: dto.Op,
        ExpectedValue: dto.ExpectedValue,
        ActualValue: dto.ActualValue,
        Passed: dto.Passed,
        Skipped: dto.Skipped,
        Group: dto.Group is not null ? FilterGroupFromDto(dto.Group) : null);

    private static IdentificationTraceDto IdentificationTraceToDto(IdentificationTrace trace) => new()
    {
        Strategy = trace.Strategy,
        Attempted = trace.Attempted,
        Detail = trace.Detail
    };

    private static IdentificationTrace IdentificationTraceFromDto(IdentificationTraceDto dto) => new(
        Strategy: dto.Strategy,
        Attempted: dto.Attempted,
        Detail: dto.Detail);

    private static TracedIdentificationDto IdentificationToDto(TracedIdentification trace) => new()
    {
        Season = trace.Season,
        Episode = trace.Episode,
        Title = trace.Title
    };

    private static TracedIdentification IdentificationFromDto(TracedIdentificationDto dto) => new(
        Season: dto.Season,
        Episode: dto.Episode,
        Title: dto.Title);
}
