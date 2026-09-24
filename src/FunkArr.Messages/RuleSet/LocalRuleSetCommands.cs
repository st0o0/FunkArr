using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Scoring;

namespace FunkArr.Messages.RuleSet;

public sealed record RuleSetBody(
    string Topic,
    string[]? Aliases,
    RuleSetMediaInput Media,
    float? Confidence,
    RuleSetRuleInput[] Rules,
    bool? Standalone,
    string[]? Disable,
    EnrichmentConfig? Enrichment);

public sealed record RuleSetMediaInput(
    string Name,
    MediaType Type,
    int? TvdbId = null,
    string? ImdbId = null,
    int? TmdbId = null);

public sealed record RuleSetRuleInput(
    string Id,
    int Priority = 0,
    float? Confidence = null,
    IdentificationStrategy? Strategy = null,
    string? SeasonRegex = null,
    string? EpisodeRegex = null,
    int? CaptureGroup = null,
    RuleSetFilterGroupInput? Filters = null,
    RuleSetTitleRuleInput[]? TitleRules = null);

public sealed record RuleSetFilterGroupInput(
    RuleSetFilterConditionInput[]? All = null,
    RuleSetFilterConditionInput[]? Any = null,
    RuleSetFilterConditionInput[]? Not = null);

public sealed record RuleSetFilterConditionInput(
    FilterField Field,
    FilterOp Op,
    string Value);

public sealed record RuleSetTitleRuleInput(
    TitlePartType Type,
    FilterField? Field = null,
    string? Pattern = null,
    int? CaptureGroup = null,
    string? Value = null);

public sealed record CreateLocalRuleSet(string RuleSetId, RuleSetBody Body) : IWithRuleSetId;

public abstract record CreateLocalRuleSetResponse;
public sealed record CreateLocalRuleSetCompleted(string RuleSetId) : CreateLocalRuleSetResponse;
public sealed record CreateLocalRuleSetFailed(CreateLocalRuleSetFailureReason Reason) : CreateLocalRuleSetResponse;

public enum CreateLocalRuleSetFailureReason { AlreadyExists }

public sealed record CreateLocalRuleSetValidationFailed(
    IReadOnlyList<string> Errors) : CreateLocalRuleSetResponse;

public sealed record UpdateLocalRuleSet(string RuleSetId, RuleSetBody Body) : IWithRuleSetId;

public abstract record UpdateLocalRuleSetResponse;
public sealed record UpdateLocalRuleSetCompleted : UpdateLocalRuleSetResponse;
public sealed record UpdateLocalRuleSetFailed(UpdateLocalRuleSetFailureReason Reason) : UpdateLocalRuleSetResponse;

public enum UpdateLocalRuleSetFailureReason { NotFound }

public sealed record UpdateLocalRuleSetValidationFailed(
    IReadOnlyList<string> Errors) : UpdateLocalRuleSetResponse;

public sealed record DeleteLocalRuleSet(string RuleSetId) : IWithRuleSetId;

public abstract record DeleteLocalRuleSetResponse;
public sealed record DeleteLocalRuleSetCompleted : DeleteLocalRuleSetResponse;
public sealed record DeleteLocalRuleSetFailed(DeleteLocalRuleSetFailureReason Reason) : DeleteLocalRuleSetResponse;

public enum DeleteLocalRuleSetFailureReason { NotFound }

public sealed record ExportRuleSet(string RuleSetId) : IWithRuleSetId;

public abstract record ExportRuleSetResponse;
public sealed record ExportRuleSetCompleted(string Json) : ExportRuleSetResponse;
public sealed record ExportRuleSetFailed(ExportRuleSetFailureReason Reason) : ExportRuleSetResponse;

public enum ExportRuleSetFailureReason { NotFound }

public sealed record ExportRuleSetValidationFailed(
    IReadOnlyList<string> Errors) : ExportRuleSetResponse;

public sealed record WorkerReady(string RuleSetId, int RuleCount, string SourceType);

public sealed record WorkerRemoved(string RuleSetId);
