using System.Diagnostics.Metrics;

namespace FunkArr.RuleSet;

internal static class Telemetry
{
    internal static readonly Meter Meter = new("FunkArr.RuleSet");

    internal static readonly Counter<long> Loaded = Meter.CreateCounter<long>(
        "funkarr.ruleset.loaded_total", description: "Total rulesets loaded");

    internal static readonly Counter<long> Removed = Meter.CreateCounter<long>(
        "funkarr.ruleset.removed_total", description: "Total rulesets removed");

    private static int _activeCount;

    internal static readonly ObservableGauge<int> Active = Meter.CreateObservableGauge(
        "funkarr.ruleset.active", () => _activeCount, description: "Active ruleset count");

    internal static readonly Counter<long> UpdateChecks = Meter.CreateCounter<long>(
        "funkarr.ruleset.update_checks_total", description: "Total community update checks");

    internal static readonly Counter<long> UpdatesApplied = Meter.CreateCounter<long>(
        "funkarr.ruleset.updates_applied_total", description: "Total community updates applied");

    internal static readonly Counter<long> UpdateErrors = Meter.CreateCounter<long>(
        "funkarr.ruleset.update_errors_total", description: "Total community update errors");

    internal static readonly Counter<long> ValidationErrors = Meter.CreateCounter<long>(
        "funkarr.ruleset.validation_errors_total", description: "Total ruleset validation errors");

    internal static readonly Counter<long> Scans = Meter.CreateCounter<long>(
        "funkarr.ruleset.scans_total", description: "Total ruleset scans");

    internal static readonly Counter<long> FileEvents = Meter.CreateCounter<long>(
        "funkarr.ruleset.file_events_total", description: "Total filesystem watcher events");

    internal static void SetActiveCount(int count) => _activeCount = count;
}
