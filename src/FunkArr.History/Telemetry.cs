using System.Diagnostics.Metrics;

namespace FunkArr.History;

internal static class Telemetry
{
    internal static readonly Meter Meter = new("FunkArr.History");

    internal static readonly Counter<long> Recordings = Meter.CreateCounter<long>(
        "funkarr.history.recordings_total", description: "Total scoring history recordings");

    internal static readonly Counter<long> Trimmed = Meter.CreateCounter<long>(
        "funkarr.history.trimmed_total", description: "Total scoring history entries trimmed");

    internal static readonly Counter<long> Queries = Meter.CreateCounter<long>(
        "funkarr.history.queries_total", description: "Total scoring history queries");
}
