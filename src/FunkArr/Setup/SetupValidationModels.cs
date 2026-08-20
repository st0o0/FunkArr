using System.Text.Json;
using System.Text.Json.Serialization;
using FunkArr.Configuration;

namespace FunkArr.Setup;

public sealed record ValidationRequest(
    ArrConnection? Prowlarr,
    List<ArrInstanceConnection>? ArrInstances,
    string? SelfUrl);

public enum CheckStatus
{
    Pass,
    Warning,
    Fail,
}

public sealed record CheckResult(
    string Category,
    string Name,
    CheckStatus Status,
    string Message,
    string? FixGuidance);

public sealed record ValidationResult(CheckStatus OverallStatus, IReadOnlyList<CheckResult> Checks)
{
    public static ValidationResult From(IReadOnlyList<CheckResult> checks) =>
        new(DeriveOverallStatus(checks), checks);

    private static CheckStatus DeriveOverallStatus(IReadOnlyList<CheckResult> checks)
    {
        if (checks.Any(c => c.Status == CheckStatus.Fail))
        {
            return CheckStatus.Fail;
        }

        if (checks.Any(c => c.Status == CheckStatus.Warning))
        {
            return CheckStatus.Warning;
        }

        return CheckStatus.Pass;
    }
}

public static class SetupValidationJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
        },
    };
}
