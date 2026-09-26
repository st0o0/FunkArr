using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FunkArr.Scoring;

internal static class Telemetry
{
    internal static readonly ActivitySource Source = new("FunkArr.Scoring");
    internal static readonly Meter Meter = new("FunkArr.Scoring");

    internal static readonly Histogram<double> ScoringDuration = Meter.CreateHistogram<double>(
        "funkarr.scoring.duration", "s", "Scoring evaluation duration in seconds");
}
