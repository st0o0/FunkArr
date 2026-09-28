using System.Diagnostics.Metrics;

namespace FunkArr.RuleSet;

internal static class Telemetry
{
    private static readonly Meter _meter = new("FunkArr.RuleSet");

    internal static readonly Counter<long> Loaded = _meter.CreateCounter<long>(
        "funkarr.ruleset.loaded_total", description: "Total rulesets loaded");

    internal static readonly Counter<long> Removed = _meter.CreateCounter<long>(
        "funkarr.ruleset.removed_total", description: "Total rulesets removed");

    private static int _activeCount;

    static Telemetry()
    {
        _meter.CreateObservableGauge("funkarr.ruleset.active", () => _activeCount, description: "Active ruleset count");
    }

    internal static readonly Counter<long> UpdateChecks = _meter.CreateCounter<long>(
        "funkarr.ruleset.update_checks_total", description: "Total community update checks");

    internal static readonly Counter<long> UpdatesApplied = _meter.CreateCounter<long>(
        "funkarr.ruleset.updates_applied_total", description: "Total community updates applied");

    internal static readonly Counter<long> UpdateErrors = _meter.CreateCounter<long>(
        "funkarr.ruleset.update_errors_total", description: "Total community update errors");

    internal static readonly Counter<long> ValidationErrors = _meter.CreateCounter<long>(
        "funkarr.ruleset.validation_errors_total", description: "Total ruleset validation errors");

    internal static readonly Counter<long> Scans = _meter.CreateCounter<long>(
        "funkarr.ruleset.scans_total", description: "Total ruleset scans");

    internal static readonly Counter<long> FileEvents = _meter.CreateCounter<long>(
        "funkarr.ruleset.file_events_total", description: "Total filesystem watcher events");

    internal static void SetActiveCount(int count) => _activeCount = count;
}
