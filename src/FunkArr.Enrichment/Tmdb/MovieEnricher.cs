using FunkArr.Messages.Enrichment;

namespace FunkArr.Enrichment.Tmdb;

public static class MovieEnricher
{
    public static EnrichedMovie[] Resolve(
        TmdbMovie movie, string[] alternativeTitles, MovieCandidate[] candidates, EnrichmentConfig? config = null)
    {
        if (config is { Enabled: false })
        {
            return [];
        }

        var year = ParseYear(movie.ReleaseDate);
        var results = new List<EnrichedMovie>();

        foreach (var candidate in candidates)
        {
            var resolved = ResolveCandidate(candidate, movie, alternativeTitles, year, config);
            if (resolved is not null)
            {
                results.Add(resolved);
            }
        }

        return [.. results];
    }

    private static EnrichedMovie? ResolveCandidate(
        MovieCandidate candidate, TmdbMovie movie, string[] altTitles, int? movieYear, EnrichmentConfig? config)
    {
        var titleThreshold = config?.Title.Threshold ?? 0.5f;

        var allTitles = new List<string>();
        if (movie.Title is not null)
        {
            allTitles.Add(movie.Title);
        }

        if (movie.OriginalTitle is not null && movie.OriginalTitle != movie.Title)
        {
            allTitles.Add(movie.OriginalTitle);
        }

        allTitles.AddRange(altTitles);

        var bestSimilarity = 0f;

        foreach (var title in allTitles)
        {
            var similarity = LevenshteinDistance.Similarity(candidate.Title, title);
            if (similarity > bestSimilarity)
            {
                bestSimilarity = similarity;
            }
        }

        if (bestSimilarity >= titleThreshold)
        {
            return new EnrichedMovie(
                candidate.Index,
                movie.Title ?? "",
                movieYear ?? 0,
                movie.ImdbId,
                movie.Id,
                bestSimilarity,
                MatchMethod.TitleMatch);
        }

        return null;
    }

    private static int? ParseYear(string? releaseDate)
    {
        if (releaseDate is null or { Length: < 4 })
        {
            return null;
        }

        return int.TryParse(releaseDate.AsSpan(0, 4), out var year) ? year : null;
    }
}
