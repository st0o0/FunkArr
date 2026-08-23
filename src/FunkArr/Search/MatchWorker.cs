using Akka.Actor;
using Akka.Event;
using FunkArr.RuleSet;
using FunkArr.Shared.Models;

namespace FunkArr.Search;

internal sealed class MatchWorker : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public MatchWorker()
    {
        Receive<MatchItems>(Handle);
    }

    private void Handle(MatchItems message)
    {
        if (message.Rules.Count > 0)
        {
            HandleRuleSetPath(message);
        }
        else
        {
            HandleGenericPath(message);
        }
    }

    private void HandleRuleSetPath(MatchItems message)
    {
        var (_, traces) = RuleSetMatchingEngine.EvaluateRulesWithTraces(
            message.Items, message.Rules, message.TvdbEpisodes, message.ShowName ?? string.Empty);

        var matchedTitles = new HashSet<string>(
            traces.OfType<MatchedTrace>().Select(t => t.ItemTitle));

        var results = new List<SearchResult>();

        foreach (var item in message.Items)
        {
            if (!matchedTitles.Contains(item.Title))
            {
                continue;
            }

            var timestamp = DateTimeOffset.FromUnixTimeSeconds(item.Timestamp);

            if (!string.IsNullOrEmpty(item.Url_Video_HD))
            {
                results.Add(new SearchResult
                {
                    Title = item.Title,
                    Topic = item.Topic,
                    Channel = item.Channel,
                    Url = item.Url_Video_HD,
                    UrlSubtitle = string.IsNullOrEmpty(item.Url_Subtitle) ? null : item.Url_Subtitle,
                    Description = string.IsNullOrEmpty(item.Description) ? null : item.Description,
                    DurationSeconds = item.Duration,
                    SizeBytes = QualityProbeService.EstimateSize(item.Duration, QualityTier.HD1080),
                    Timestamp = timestamp,
                    Quality = QualityTier.HD1080,
                });
            }

            if (!string.IsNullOrEmpty(item.Url_Video))
            {
                results.Add(new SearchResult
                {
                    Title = item.Title,
                    Topic = item.Topic,
                    Channel = item.Channel,
                    Url = item.Url_Video,
                    UrlSubtitle = string.IsNullOrEmpty(item.Url_Subtitle) ? null : item.Url_Subtitle,
                    Description = string.IsNullOrEmpty(item.Description) ? null : item.Description,
                    DurationSeconds = item.Duration,
                    SizeBytes = QualityProbeService.EstimateSize(item.Duration, QualityTier.HD720),
                    Timestamp = timestamp,
                    Quality = QualityTier.HD720,
                });
            }

            if (!string.IsNullOrEmpty(item.Url_Video_Low))
            {
                results.Add(new SearchResult
                {
                    Title = item.Title,
                    Topic = item.Topic,
                    Channel = item.Channel,
                    Url = item.Url_Video_Low,
                    UrlSubtitle = string.IsNullOrEmpty(item.Url_Subtitle) ? null : item.Url_Subtitle,
                    Description = string.IsNullOrEmpty(item.Description) ? null : item.Description,
                    DurationSeconds = item.Duration,
                    SizeBytes = QualityProbeService.EstimateSize(item.Duration, QualityTier.SD),
                    Timestamp = timestamp,
                    Quality = QualityTier.SD,
                });
            }
        }

        var record = new MatchRecord
        {
            Id = Guid.NewGuid().ToString("N")[..10],
            Timestamp = DateTimeOffset.UtcNow,
            SearchTopic = message.ShowName ?? string.Empty,
            TvdbId = null,
            Season = message.Context.Season,
            Episode = message.Context.Episode,
            Source = "ruleset",
            TotalResults = message.Items.Length,
            Matched = traces.OfType<MatchedTrace>().ToList(),
            Filtered = traces.OfType<FilteredTrace>().ToList(),
            Unmatched = traces.OfType<UnmatchedTrace>().ToList(),
        };

        _log.Debug(
            "RuleSet match: {Matched} matched, {Filtered} filtered, {Unmatched} unmatched out of {Total}",
            record.Matched.Count, record.Filtered.Count, record.Unmatched.Count, message.Items.Length);

        Sender.Tell(new ItemsMatched(results, record));
    }

    private void HandleGenericPath(MatchItems message)
    {
        var results = MatchingPipeline.Execute(message.Items, message.Context);

        var record = SearchChildHelpers.BuildGenericPipelineRecord(
            message.ShowName ?? message.Context.ShowName ?? string.Empty,
            null,
            message.Context.Season,
            message.Context.Episode,
            message.Items.Length);

        _log.Debug(
            "Generic pipeline: {Matched}/{Total} results",
            results.Count, message.Items.Length);

        Sender.Tell(new ItemsMatched(results, record));
    }
}
