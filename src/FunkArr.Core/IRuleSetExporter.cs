namespace FunkArr.Core;

public sealed record RuleSetExportResult(
    bool Success,
    string? Json = null,
    IReadOnlyList<RuleSetValidationError>? Errors = null,
    string? Error = null);

public interface IRuleSetExporter
{
    RuleSetExportResult Export(string ruleSetId);
}
