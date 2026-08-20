using System.Globalization;
using Akka.Actor;
using Akka.Event;
using FunkArr.RuleSet;
using FunkArr.Shared.Models;

namespace FunkArr.Search;

internal sealed class TvSearchActor : ReceiveActor
{
    private readonly MediathekClient _mediathekClient;
    private readonly TvdbClient _tvdbClient;
    private readonly QualityProbeService _qualityProbeService;
    private readonly int _probeLimit;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public TvSearchActor(
        MediathekClient mediathekClient,
        TvdbClient tvdbClient,
        QualityProbeService qualityProbeService,
        int probeLimit)
    {
        _mediathekClient = mediathekClient;
        _tvdbClient = tvdbClient;
        _qualityProbeService = qualityProbeService;
        _probeLimit = probeLimit;

        ReceiveAsync<ExecuteTvSearch>(HandleAsync);
    }

    private async Task HandleAsync(ExecuteTvSearch command)
    {
        var request = command.Request;
        var results = await SearchChildHelpers.SearchMediathekAsync(_mediathekClient, _log, command.SearchTerm);

        IReadOnlyList<SearchResult> filtered;
        MatchRecord matchRecord;

        if (command.Rules.Count > 0)
        {
            (filtered, matchRecord) = await ApplyRuleSetMatchingWithTraces(
                results, command.Rules,
                await GetTvdbEpisodesAsync(request.TvdbId, request.Season),
                command.ShowName ?? command.SearchTerm, command.SearchTerm, request.TvdbId,
                request.Season, request.Episode, "ruleset");
        }
        else
        {
            DateTimeOffset? airDate = null;

            if (request.Season is not null && request.Episode is not null && request.TvdbId > 0)
            {
                var episodes = await _tvdbClient.GetEpisodesAsync(request.TvdbId, request.Season.Value);
                var ep = episodes?.FirstOrDefault(e =>
                    e.AiredSeason == request.Season && e.AiredEpisodeNumber == request.Episode);
                if (ep is not null && DateTime.TryParseExact(
                        ep.FirstAired, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    airDate = new DateTimeOffset(date, TimeSpan.Zero);
                }
            }

            var context = new MatchContext
            {
                ShowName = command.ShowName,
                Season = request.Season,
                Episode = request.Episode,
                AirDate = airDate,
            };
            filtered = await MatchingPipeline.ExecuteAsync(results, context, _qualityProbeService, _probeLimit);

            matchRecord = SearchChildHelpers.BuildGenericPipelineRecord(
                command.SearchTerm, request.TvdbId, request.Season, request.Episode, results.Length);

            _log.Debug(
                "Generic pipeline result for '{Topic}': {Matched}/{Total} results",
                command.SearchTerm, filtered.Count, results.Length);
        }

        Sender.Tell(new SearchCompleted(command.CacheKey, filtered, matchRecord, command.ReplyTo));
    }

    private async Task<(IReadOnlyList<SearchResult> Results, MatchRecord Record)> ApplyRuleSetMatchingWithTraces(
        MediathekResultItem[] items,
        IReadOnlyList<Rule> rules,
        TvdbEpisodeInfo[] tvdbEpisodes,
        string showName,
        string searchTopic,
        int? tvdbId,
        int? season,
        int? episode,
        string source)
    {
        var (_, traces) = RuleSetMatchingEngine.EvaluateRulesWithTraces(
            items, rules, tvdbEpisodes, showName);

        var matchedItems = new HashSet<string>(
            traces.OfType<MatchedTrace>().Select(t => t.ItemTitle));

        var searchResults = new List<SearchResult>();
        var count = 0;
        foreach (var item in items)
        {
            if (matchedItems.Contains(item.Title))
            {
                var variants = await _qualityProbeService.ExpandWithProbingAsync(item, _probeLimit, count);
                searchResults.AddRange(variants);
                count += variants.Count;
            }
        }

        var record = new MatchRecord
        {
            Id = Guid.NewGuid().ToString("N")[..10],
            Timestamp = DateTimeOffset.UtcNow,
            SearchTopic = searchTopic,
            TvdbId = tvdbId,
            Season = season,
            Episode = episode,
            Source = source,
            TotalResults = items.Length,
            Matched = traces.OfType<MatchedTrace>().ToList(),
            Filtered = traces.OfType<FilteredTrace>().ToList(),
            Unmatched = traces.OfType<UnmatchedTrace>().ToList(),
        };

        _log.Debug(
            "Match result for '{Topic}': {Matched} matched, {Filtered} filtered, {Unmatched} unmatched out of {Total}",
            searchTopic, record.Matched.Count, record.Filtered.Count, record.Unmatched.Count, items.Length);

        var orderedResults = searchResults
            .OrderByDescending(r => r.Quality)
            .ToList();

        return (orderedResults, record);
    }

    private async Task<TvdbEpisodeInfo[]> GetTvdbEpisodesAsync(int tvdbId, int? season)
    {
        if (tvdbId <= 0)
        {
            return [];
        }

        if (season is not null)
        {
            var episodes = await _tvdbClient.GetEpisodesAsync(tvdbId, season.Value);
            return episodes ?? [];
        }

        var allEpisodes = await _tvdbClient.GetEpisodesAsync(tvdbId, 1);
        return allEpisodes ?? [];
    }
}
