using FunkArr.Messages.MetadataResolver;

namespace FunkArr.MetadataResolver;

internal static class MovieResolver
{
    public static MovieResolved[] Resolve(
        TmdbMovie movie, string[] alternativeTitles, MovieCandidate[] candidates)
    {
        var year = ParseYear(movie.ReleaseDate);
        var results = new List<MovieResolved>();

        foreach (var candidate in candidates)
        {
            var resolved = ResolveCandidate(candidate, movie, alternativeTitles, year);
            if (resolved is not null)
            {
                results.Add(resolved);
            }
        }

        return results.ToArray();
    }

    private static MovieResolved? ResolveCandidate(
        MovieCandidate candidate, TmdbMovie movie, string[] altTitles, int? movieYear)
    {
        var allTitles = new List<string>();
        if (movie.Title is not null) allTitles.Add(movie.Title);
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

        var yearMatches = ValidateYear(candidate.AiredAt, movieYear);

        if (bestSimilarity >= 0.5f && yearMatches)
        {
            return new MovieResolved(
                candidate.Index,
                movie.Title ?? "",
                movieYear ?? 0,
                movie.ImdbId,
                movie.Id,
                bestSimilarity,
                "TitleMatch");
        }

        if (movieYear is not null && bestSimilarity >= 0.3f &&
            candidate.AiredAt is not null && candidate.AiredAt.Value.Year == movieYear.Value)
        {
            return new MovieResolved(
                candidate.Index,
                movie.Title ?? "",
                movieYear.Value,
                movie.ImdbId,
                movie.Id,
                bestSimilarity * 0.8f,
                "YearMatch");
        }

        return null;
    }

    private static bool ValidateYear(DateTimeOffset? candidateAiredAt, int? movieYear)
    {
        if (movieYear is null || candidateAiredAt is null)
        {
            return true;
        }

        return Math.Abs(candidateAiredAt.Value.Year - movieYear.Value) <= 1;
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
