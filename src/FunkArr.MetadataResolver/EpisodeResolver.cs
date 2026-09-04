using System.Globalization;
using FunkArr.Messages.MetadataResolver;

namespace FunkArr.MetadataResolver;

internal static class EpisodeResolver
{
    public static ResolvedEpisode[] Resolve(
        TvdbEpisode[] tvdbEpisodes, EpisodeCandidate[] candidates, ResolutionConfig config)
    {
        if (config.Strategy == "none") return [];

        var threshold = config.Strategy == "strict" ? 0.95f : config.Threshold;
        var results = new List<ResolvedEpisode>();

        foreach (var candidate in candidates)
        {
            var resolved = ResolveCandidate(candidate, tvdbEpisodes, threshold, config.AirdateTolerance);
            if (resolved is not null)
            {
                results.Add(resolved);
            }
        }

        return results.ToArray();
    }

    private static ResolvedEpisode? ResolveCandidate(
        EpisodeCandidate candidate, TvdbEpisode[] episodes, float threshold, int airdateTolerance)
    {
        if (candidate.ExistingSeason is not null && candidate.ExistingEpisode is not null)
        {
            var tvdbMatch = FindBySeasonEpisode(episodes, candidate.ExistingSeason, candidate.ExistingEpisode);
            return new ResolvedEpisode(candidate.Index,
                candidate.ExistingSeason, candidate.ExistingEpisode,
                tvdbMatch?.Name ?? "", 1.0f, "RegexExtracted");
        }

        var titleMatch = FindByTitle(candidate, episodes, threshold);
        if (titleMatch is not null)
        {
            return titleMatch;
        }

        var airdateMatch = FindByAirdate(candidate, episodes, airdateTolerance);
        if (airdateMatch is not null)
        {
            return airdateMatch;
        }

        return null;
    }

    private static TvdbEpisode? FindBySeasonEpisode(
        TvdbEpisode[] episodes, string season, string episode)
    {
        if (!int.TryParse(season, CultureInfo.InvariantCulture, out var s) ||
            !int.TryParse(episode, CultureInfo.InvariantCulture, out var e))
        {
            return null;
        }

        return Array.Find(episodes, ep => ep.SeasonNumber == s && ep.Number == e);
    }

    private static ResolvedEpisode? FindByTitle(
        EpisodeCandidate candidate, TvdbEpisode[] episodes, float threshold)
    {
        TvdbEpisode? bestMatch = null;
        var bestSimilarity = 0f;

        foreach (var episode in episodes)
        {
            if (string.IsNullOrEmpty(episode.Name))
            {
                continue;
            }

            var similarity = LevenshteinDistance.Similarity(candidate.Title, episode.Name);

            if (candidate.ConstructedTitle is not null)
            {
                var constructedSimilarity = LevenshteinDistance.Similarity(candidate.ConstructedTitle, episode.Name);
                similarity = Math.Max(similarity, constructedSimilarity);
            }

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
                .Select(ep =>
                {
                    var sim = LevenshteinDistance.Similarity(candidate.Title, ep.Name!);
                    if (candidate.ConstructedTitle is not null)
                    {
                        sim = Math.Max(sim, LevenshteinDistance.Similarity(candidate.ConstructedTitle, ep.Name!));
                    }

                    return (Episode: ep, Similarity: sim);
                })
                .Where(x => Math.Abs(x.Similarity - bestSimilarity) < 0.001f)
                .ToArray();

            if (tieBreakers.Length > 1)
            {
                bestMatch = BreakTieByRuntime(tieBreakers.Select(t => t.Episode).ToArray(), candidate.Duration)
                            ?? bestMatch;
            }
        }

        return new ResolvedEpisode(
            candidate.Index,
            bestMatch.SeasonNumber.ToString(CultureInfo.InvariantCulture),
            bestMatch.Number.ToString(CultureInfo.InvariantCulture),
            bestMatch.Name ?? "",
            bestSimilarity,
            "FuzzyTitleMatch");
    }

    private static ResolvedEpisode? FindByAirdate(
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

        return new ResolvedEpisode(
            candidate.Index,
            bestMatch.SeasonNumber.ToString(CultureInfo.InvariantCulture),
            bestMatch.Number.ToString(CultureInfo.InvariantCulture),
            bestMatch.Name ?? "",
            Math.Max(confidence, 0.1f),
            "AirdateMatch");
    }

    private static TvdbEpisode? BreakTieByRuntime(TvdbEpisode[] candidates, int durationSeconds)
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
            var tolerance = epDurationSeconds * 0.35;

            if (diff <= tolerance && diff < bestDiff)
            {
                bestDiff = diff;
                best = ep;
            }
        }

        return best;
    }
}
