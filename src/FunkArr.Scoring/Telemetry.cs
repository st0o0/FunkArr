using System.Diagnostics.Metrics;

namespace FunkArr.Scoring;

internal static class Telemetry
{
    private static readonly Meter _meter = new("FunkArr.Scoring");

    internal static readonly Counter<long> Accepted = _meter.CreateCounter<long>(
        "funkarr.scoring.accepted_total", description: "Results accepted by scoring");

    internal static readonly Counter<long> Rejected = _meter.CreateCounter<long>(
        "funkarr.scoring.rejected_total", description: "Results rejected by scoring");

    internal static readonly Histogram<double> Duration = _meter.CreateHistogram<double>(
        "funkarr.scoring.duration_seconds", unit: "s", description: "Scoring duration");

    internal static readonly Counter<long> Runs = _meter.CreateCounter<long>(
        "funkarr.scoring.runs_total", description: "Total scoring invocations");

    internal static readonly Counter<long> RegexTimeouts = _meter.CreateCounter<long>(
        "funkarr.scoring.regex_timeouts_total", description: "Regex match timeouts");

    internal static readonly Counter<long> NoConfig = _meter.CreateCounter<long>(
        "funkarr.scoring.no_config_total", description: "Scoring requests with no matching config");
}
