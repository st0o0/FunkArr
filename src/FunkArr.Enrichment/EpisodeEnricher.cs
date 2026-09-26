using System.Globalization;
using FunkArr.Messages.Enrichment;

namespace FunkArr.Enrichment;

public static class EpisodeEnricher
{
    public static EnrichedEpisode[] Resolve(
        TvdbEpisode[] tvdbEpisodes, EpisodeCandidate[] candidates, EnrichmentConfig? config = null)
    {
        if (config is { Enabled: false })
        {
            return [];
        }

        var results = new List<EnrichedEpisode>();

        foreach (var candidate in candidates)
        {
            var resolved = ResolveCandidate(candidate, tvdbEpisodes, config);
            if (resolved is not null)
            {
                results.Add(resolved);
            }
        }

        return [.. results];
    }

    private static EnrichedEpisode? ResolveCandidate(
        EpisodeCandidate candidate, TvdbEpisode[] episodes, EnrichmentConfig? config)
    {
        var titleThreshold = config?.Title.Threshold ?? 0.7f;
        var airdateTolerance = config?.Airdate.Tolerance ?? 7;
        var runtimeTolerance = config?.Runtime.Tolerance ?? 0.35f;
        var runtimeMode = config?.Runtime.Mode ?? RuntimeMode.Tiebreaker;
        var methods = config?.Methods ?? [EnrichmentMethod.Title, EnrichmentMethod.Airdate];

        var filteredEpisodes = runtimeMode == RuntimeMode.Filter
            ? FilterByRuntime(episodes, candidate.Duration, runtimeTolerance)
            : episodes;

        var minTitleAffinity = config?.Airdate.MinTitleAffinity ?? 0.3f;
        var bestTitleScore = -1f;

        foreach (var method in methods)
        {
            var result = method switch
            {
                EnrichmentMethod.Title => FindByTitle(candidate, filteredEpisodes, titleThreshold, runtimeTolerance),
                EnrichmentMethod.Airdate => FindByAirdateGuarded(candidate, filteredEpisodes, airdateTolerance, minTitleAffinity, ref bestTitleScore),
                _ => null,
            };

            if (result is not null)
            {
                return result;
            }
        }

        if (candidate.ExistingSeason is not null && candidate.ExistingEpisode is not null)
        {
            return new EnrichedEpisode(candidate.Index,
                candidate.ExistingSeason, candidate.ExistingEpisode,
                FindBySeasonEpisode(episodes, candidate.ExistingSeason, candidate.ExistingEpisode)?.Name ?? "",
                1.0f, MatchMethod.RegexExtracted);
        }

        return null;
    }

    private static TvdbEpisode[] FilterByRuntime(TvdbEpisode[] episodes, int durationSeconds, float tolerance)
    {
        var filtered = episodes.Where(ep =>
        {
            if (ep.Runtime is null or 0)
            {
                return true;
            }

            var epDurationSeconds = ep.Runtime.Value * 60;
            var diff = Math.Abs(durationSeconds - epDurationSeconds);
            return diff <= epDurationSeconds * tolerance;
        }).ToArray();

        return filtered.Length > 0 ? filtered : episodes;
    }

    private static TvdbEpisode? FindBySeasonEpisode(TvdbEpisode[] episodes, string season, string episode)
    {
        if (!int.TryParse(season, CultureInfo.InvariantCulture, out var s) ||
            !int.TryParse(episode, CultureInfo.InvariantCulture, out var e))
        {
            return null;
        }

        return Array.Find(episodes, ep => ep.SeasonNumber == s && ep.Number == e);
    }

    private static readonly string[] _titleSeparator = [" - "];

    private static float CandidateEpisodeSimilarity(string candidateTitle, string? constructedTitle, string episodeName)
    {
        var best = SimilarityWithSegments(candidateTitle, episodeName);

        if (constructedTitle is not null)
        {
            best = Math.Max(best, SimilarityWithSegments(constructedTitle, episodeName));
        }

        return best;
    }

    private static float SimilarityWithSegments(string candidate, string episodeName)
    {
        var best = LevenshteinDistance.Similarity(candidate, episodeName);
        if (best >= 1.0f)
        {
            return best;
        }

        var segments = episodeName.Split(_titleSeparator, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length <= 1)
        {
            return best;
        }

        foreach (var segment in segments)
        {
            var trimmed = segment.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            best = Math.Max(best, LevenshteinDistance.Similarity(candidate, trimmed));
            if (best >= 1.0f)
            {
                return best;
            }
        }

        return best;
    }

    internal static float BestTitleScore(EpisodeCandidate candidate, TvdbEpisode[] episodes)
    {
        var best = 0f;
        foreach (var episode in episodes)
        {
            if (string.IsNullOrEmpty(episode.Name))
            {
                continue;
            }

            best = Math.Max(best, CandidateEpisodeSimilarity(candidate.Title, candidate.ConstructedTitle, episode.Name));
            if (best >= 1.0f)
            {
                return best;
            }
        }

        return best;
    }

    private static EnrichedEpisode? FindByTitle(
        EpisodeCandidate candidate, TvdbEpisode[] episodes, float threshold, float runtimeTolerance)
    {
        TvdbEpisode? bestMatch = null;
        var bestSimilarity = 0f;

        foreach (var episode in episodes)
        {
            if (string.IsNullOrEmpty(episode.Name))
            {
                continue;
            }

            var similarity = CandidateEpisodeSimilarity(candidate.Title, candidate.ConstructedTitle, episode.Name);

            if (similarity > bestSimilarity)
            {
                bestSimilarity = similarity;
                bestMatch = episode;
            }
        }

        if (bestMatch is null || bestSimilarity < threshold)
        {
            return null;
        }

        if (bestSimilarity < 1.0f)
        {
            var tieBreakers = episodes
                .Where(ep => !string.IsNullOrEmpty(ep.Name))
                .Select(ep => (Episode: ep, Similarity: CandidateEpisodeSimilarity(candidate.Title, candidate.ConstructedTitle, ep.Name!)))
                .Where(x => Math.Abs(x.Similarity - bestSimilarity) < 0.001f)
                .ToArray();

            if (tieBreakers.Length > 1)
            {
                bestMatch = BreakTieByRuntime([.. tieBreakers.Select(t => t.Episode)], candidate.Duration, runtimeTolerance)
                            ?? bestMatch;
            }
        }

        return new EnrichedEpisode(
            candidate.Index,
            bestMatch.SeasonNumber.ToString(CultureInfo.InvariantCulture),
            bestMatch.Number.ToString(CultureInfo.InvariantCulture),
            bestMatch.Name ?? "",
            bestSimilarity,
            MatchMethod.TitleMatch);
    }

    private static EnrichedEpisode? FindByAirdateGuarded(
        EpisodeCandidate candidate, TvdbEpisode[] episodes, int toleranceDays,
        float minTitleAffinity, ref float bestTitleScore)
    {
        if (minTitleAffinity > 0f)
        {
            if (bestTitleScore < 0f)
            {
                bestTitleScore = BestTitleScore(candidate, episodes);
            }

            if (bestTitleScore < minTitleAffinity)
            {
                return null;
            }
        }

        return FindByAirdate(candidate, episodes, toleranceDays);
    }

    private static EnrichedEpisode? FindByAirdate(
        EpisodeCandidate candidate, TvdbEpisode[] episodes, int toleranceDays)
    {
        if (candidate.AiredAt is null)
        {
            return null;
        }

        var candidateDate = DateOnly.FromDateTime(candidate.AiredAt.Value.UtcDateTime);
        TvdbEpisode? bestMatch = null;
        var bestDaysDiff = int.MaxValue;

        foreach (var episode in episodes)
        {
            if (string.IsNullOrEmpty(episode.Aired))
            {
                continue;
            }

            if (!DateOnly.TryParseExact(episode.Aired, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var episodeDate))
            {
                continue;
            }

            var daysDiff = Math.Abs(candidateDate.DayNumber - episodeDate.DayNumber);
            if (daysDiff <= toleranceDays && daysDiff < bestDaysDiff)
            {
                bestDaysDiff = daysDiff;
                bestMatch = episode;
            }
        }

        if (bestMatch is null)
        {
            return null;
        }

        var confidence = toleranceDays > 0
            ? 1.0f - (float)bestDaysDiff / toleranceDays
            : 1.0f;

        return new EnrichedEpisode(
            candidate.Index,
            bestMatch.SeasonNumber.ToString(CultureInfo.InvariantCulture),
            bestMatch.Number.ToString(CultureInfo.InvariantCulture),
            bestMatch.Name ?? "",
            Math.Max(confidence, 0.1f),
            MatchMethod.AirdateMatch);
    }

    private static TvdbEpisode? BreakTieByRuntime(TvdbEpisode[] candidates, int durationSeconds, float runtimeTolerance)
    {
        TvdbEpisode? best = null;
        var bestDiff = double.MaxValue;

        foreach (var ep in candidates)
        {
            if (ep.Runtime is null or 0)
            {
                continue;
            }

            var epDurationSeconds = ep.Runtime.Value * 60;
            var diff = Math.Abs(durationSeconds - epDurationSeconds);
            var tolerance = epDurationSeconds * (double)runtimeTolerance;

            if (diff <= tolerance && diff < bestDiff)
            {
                bestDiff = diff;
                best = ep;
            }
        }

        return best;
    }
}
