using System.Diagnostics.Metrics;

namespace FunkArr.Scoring;

internal static class Telemetry
{
    internal static readonly Meter Meter = new("FunkArr.Scoring");

    internal static readonly Counter<long> Accepted = Meter.CreateCounter<long>(
        "funkarr.scoring.accepted_total", description: "Results accepted by scoring");

    internal static readonly Counter<long> Rejected = Meter.CreateCounter<long>(
        "funkarr.scoring.rejected_total", description: "Results rejected by scoring");
}
