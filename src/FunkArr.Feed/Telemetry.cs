using System.Diagnostics.Metrics;

namespace FunkArr.Feed;

internal static class Telemetry
{
    private static readonly Meter _meter = new("FunkArr.Feed");

    internal static readonly Counter<long> Evaluations = _meter.CreateCounter<long>(
        "funkarr.feed.evaluations_total", description: "Evaluations of Mediathek entries by the evaluation workers");

    internal static readonly Histogram<double> EvaluationDuration = _meter.CreateHistogram<double>(
        "funkarr.feed.evaluation_duration_seconds", unit: "s", description: "Evaluation duration from ruleset resolution to releases");

    internal static readonly Counter<long> Crawls = _meter.CreateCounter<long>(
        "funkarr.feed.crawls_total", description: "Feed ingest runs by outcome");

    internal static readonly Counter<long> ProbesUnchanged = _meter.CreateCounter<long>(
        "funkarr.feed.probes_unchanged_total", description: "Feed ingest runs that found no new Filmliste update");

    internal static readonly Counter<long> ItemsSeen = _meter.CreateCounter<long>(
        "funkarr.feed.items_seen_total", description: "New Mediathek entries collected by the feed ingest");

    internal static readonly Counter<long> Unmatched = _meter.CreateCounter<long>(
        "funkarr.feed.unmatched_total", description: "New Mediathek entries without a matching ruleset");

    internal static readonly Counter<long> Published = _meter.CreateCounter<long>(
        "funkarr.feed.published_total", description: "Releases published to the feed");

    internal static readonly Counter<long> Rejected = _meter.CreateCounter<long>(
        "funkarr.feed.rejected_total", description: "Releases rejected by the feed gate");

    internal static readonly Counter<long> Gaps = _meter.CreateCounter<long>(
        "funkarr.feed.gaps_total", description: "Feed ingest runs that could not cover the whole window");

    internal static readonly Histogram<double> CrawlDuration = _meter.CreateHistogram<double>(
        "funkarr.feed.crawl_duration_seconds", unit: "s", description: "Feed ingest run duration");

    private static int _entries;

    static Telemetry()
    {
        _meter.CreateObservableGauge("funkarr.feed.entries", () => _entries, description: "Releases currently in the feed");
    }

    internal static void SetEntries(int count) => _entries = count;
}
