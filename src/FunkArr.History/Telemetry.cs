using System.Diagnostics.Metrics;

namespace FunkArr.History;

internal static class Telemetry
{
    private static readonly Meter _meter = new("FunkArr.History");

    internal static readonly Counter<long> Recordings = _meter.CreateCounter<long>(
        "funkarr.history.recordings_total", description: "Total scoring history recordings");

    internal static readonly Counter<long> Trimmed = _meter.CreateCounter<long>(
        "funkarr.history.trimmed_total", description: "Total scoring history entries trimmed");

    internal static readonly Counter<long> Queries = _meter.CreateCounter<long>(
        "funkarr.history.queries_total", description: "Total scoring history queries");
}
