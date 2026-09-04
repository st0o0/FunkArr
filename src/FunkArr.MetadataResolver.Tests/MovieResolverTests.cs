using FunkArr.Messages.MetadataResolver;

namespace FunkArr.MetadataResolver.Tests;

public sealed class MovieResolverTests
{
    private static readonly TmdbMovie _fightClub = new(
        Id: 550, Title: "Fight Club", OriginalTitle: "Fight Club",
        ReleaseDate: "1999-10-15", Runtime: 139, ImdbId: "tt0137523");

    private static readonly string[] _fightClubAltTitles = ["Clube da Luta", "El club de la pelea"];

    [Fact]
    public void Exact_title_match_returns_high_confidence()
    {
        var candidates = new[] { new MovieCandidate(0, "Fight Club", null, 8340) };

        var results = MovieResolver.Resolve(_fightClub, [], candidates);

        Assert.Single(results);
        Assert.Equal(0, results[0].Index);
        Assert.Equal("Fight Club", results[0].Title);
        Assert.Equal(1999, results[0].Year);
        Assert.Equal("tt0137523", results[0].ImdbId);
        Assert.Equal(550, results[0].TmdbId);
        Assert.True(results[0].Confidence > 0.95f);
        Assert.Equal("TitleMatch", results[0].Strategy);
    }

    [Fact]
    public void Fuzzy_title_match_returns_medium_confidence()
    {
        var candidates = new[] { new MovieCandidate(0, "Fight Club 1999", null, 8340) };

        var results = MovieResolver.Resolve(_fightClub, [], candidates);

        Assert.Single(results);
        Assert.True(results[0].Confidence >= 0.5f);
        Assert.Equal("TitleMatch", results[0].Strategy);
    }

    [Fact]
    public void Alternative_title_match_works()
    {
        var candidates = new[] { new MovieCandidate(0, "Clube da Luta", null, 8340) };

        var results = MovieResolver.Resolve(_fightClub, _fightClubAltTitles, candidates);

        Assert.Single(results);
        Assert.Equal("Fight Club", results[0].Title);
        Assert.True(results[0].Confidence > 0.9f);
    }

    [Fact]
    public void Year_mismatch_rejects_match()
    {
        var wrongYear = new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var candidates = new[] { new MovieCandidate(0, "Fight Club", wrongYear, 8340) };

        var movie = _fightClub with { ReleaseDate = "2020-01-01" };
        var results = MovieResolver.Resolve(movie, [], candidates);

        Assert.Empty(results);
    }

    [Fact]
    public void No_similar_title_returns_empty()
    {
        var candidates = new[] { new MovieCandidate(0, "Completely Different Movie", null, 8340) };

        var results = MovieResolver.Resolve(_fightClub, [], candidates);

        Assert.Empty(results);
    }

    [Fact]
    public void Year_only_match_low_title_similarity()
    {
        var sameYear = new DateTimeOffset(1999, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var candidates = new[] { new MovieCandidate(0, "Fightclub - Der Untergrund", sameYear, 8340) };

        var results = MovieResolver.Resolve(_fightClub, [], candidates);

        Assert.Single(results);
        Assert.Equal("YearMatch", results[0].Strategy);
        Assert.True(results[0].Confidence < 0.8f);
    }

    [Fact]
    public void Year_tolerance_allows_one_year_off()
    {
        var closeYear = new DateTimeOffset(2000, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var candidates = new[] { new MovieCandidate(0, "Fight Club", closeYear, 8340) };

        var results = MovieResolver.Resolve(_fightClub, [], candidates);

        Assert.Single(results);
        Assert.Equal(1999, results[0].Year);
    }

    [Fact]
    public void Null_release_date_skips_year_validation()
    {
        var movie = _fightClub with { ReleaseDate = null };
        var candidates = new[] { new MovieCandidate(0, "Fight Club", null, 8340) };

        var results = MovieResolver.Resolve(movie, [], candidates);

        Assert.Single(results);
        Assert.Equal(0, results[0].Year);
    }

    [Fact]
    public void Multiple_candidates_resolved_independently()
    {
        var candidates = new[]
        {
            new MovieCandidate(0, "Fight Club", null, 8340),
            new MovieCandidate(1, "Something Else", null, 5400),
            new MovieCandidate(2, "Clube da Luta", null, 8340),
        };

        var results = MovieResolver.Resolve(_fightClub, _fightClubAltTitles, candidates);

        Assert.Equal(2, results.Length);
        Assert.Contains(results, r => r.Index == 0);
        Assert.Contains(results, r => r.Index == 2);
    }
}
