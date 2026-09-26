using System.Diagnostics.Metrics;

namespace FunkArr.Scoring;

internal static class Telemetry
{
    internal static readonly Meter Meter = new("FunkArr.Scoring");

    internal static readonly Counter<long> Accepted = Meter.CreateCounter<long>(
        "funkarr.scoring.accepted_total", description: "Results accepted by scoring");

    internal static readonly Counter<long> Rejected = Meter.CreateCounter<long>(
        "funkarr.scoring.rejected_total", description: "Results rejected by scoring");

    internal static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(
        "funkarr.scoring.duration_seconds", unit: "s", description: "Scoring duration");

    internal static readonly Counter<long> Runs = Meter.CreateCounter<long>(
        "funkarr.scoring.runs_total", description: "Total scoring invocations");

    internal static readonly Counter<long> RegexTimeouts = Meter.CreateCounter<long>(
        "funkarr.scoring.regex_timeouts_total", description: "Regex match timeouts");

    internal static readonly Counter<long> NoConfig = Meter.CreateCounter<long>(
        "funkarr.scoring.no_config_total", description: "Scoring requests with no matching config");
}
