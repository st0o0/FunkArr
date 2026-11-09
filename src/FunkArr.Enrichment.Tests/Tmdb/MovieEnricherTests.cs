using FunkArr.Enrichment.Tmdb;
using FunkArr.Messages.Enrichment;
namespace FunkArr.Enrichment.Tests.Tmdb;

public sealed class MovieEnricherTests
{
    private static readonly TmdbMovie _fightClub = new(
        Id: 550, Title: "Fight Club", OriginalTitle: "Fight Club",
        ReleaseDate: "1999-10-15", Runtime: 139, ImdbId: "tt0137523");

    private static readonly string[] _fightClubAltTitles = ["Clube da Luta", "El club de la pelea"];

    [Fact]
    public void Exact_title_match_returns_high_confidence()
    {
        var candidates = new[] { new MovieCandidate(0, "Fight Club", null, 8340) };

        var results = MovieEnricher.Resolve(_fightClub, [], candidates);

        var item = Assert.Single(results);
        Assert.Equal(0, item.Index);
        Assert.Equal("Fight Club", item.Title);
        Assert.Equal(1999, item.Year);
        Assert.Equal("tt0137523", item.ImdbId);
        Assert.Equal(550, item.TmdbId);
        Assert.True(item.Confidence > 0.95f);
        Assert.Equal(MatchMethod.TitleMatch, item.Method);
    }

    [Fact]
    public void Fuzzy_title_match_returns_medium_confidence()
    {
        var candidates = new[] { new MovieCandidate(0, "Fight Club 1999", null, 8340) };

        var results = MovieEnricher.Resolve(_fightClub, [], candidates);

        var item = Assert.Single(results);
        Assert.True(item.Confidence >= 0.5f);
        Assert.Equal(MatchMethod.TitleMatch, item.Method);
    }

    [Fact]
    public void Alternative_title_match_works()
    {
        var candidates = new[] { new MovieCandidate(0, "Clube da Luta", null, 8340) };

        var results = MovieEnricher.Resolve(_fightClub, _fightClubAltTitles, candidates);

        var item = Assert.Single(results);
        Assert.Equal("Fight Club", item.Title);
        Assert.True(item.Confidence > 0.9f);
    }

    [Fact]
    public void Rebroadcast_with_large_year_gap_matches_on_title()
    {
        var broadcastYear = new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero);
        var candidates = new[] { new MovieCandidate(0, "Fight Club", broadcastYear, 8340) };

        var results = MovieEnricher.Resolve(_fightClub, [], candidates);

        var item = Assert.Single(results);
        Assert.Equal(1999, item.Year);
        Assert.Equal(MatchMethod.TitleMatch, item.Method);
    }

    [Fact]
    public void No_similar_title_returns_empty()
    {
        var candidates = new[] { new MovieCandidate(0, "Completely Different Movie", null, 8340) };

        var results = MovieEnricher.Resolve(_fightClub, [], candidates);

        Assert.Empty(results);
    }

    [Fact]
    public void Null_release_date_sets_year_zero()
    {
        var movie = _fightClub with { ReleaseDate = null };
        var candidates = new[] { new MovieCandidate(0, "Fight Club", null, 8340) };

        var results = MovieEnricher.Resolve(movie, [], candidates);

        var item = Assert.Single(results);
        Assert.Equal(0, item.Year);
    }

    [Fact]
    public void Multiple_candidates_resolved_independently()
    {
        var candidates = new[]
        {
            new MovieCandidate(0, "Fight Club", null, 8340),
            new MovieCandidate(1, "Something Else", null, 5400),
            new MovieCandidate(2, "Clube da Luta", null, 8340)
        };

        var results = MovieEnricher.Resolve(_fightClub, _fightClubAltTitles, candidates);

        Assert.Equal(2, results.Length);
        Assert.Contains(results, r => r.Index == 0);
        Assert.Contains(results, r => r.Index == 2);
    }

    [Fact]
    public void Disabled_enrichment_returns_empty()
    {
        var candidates = new[] { new MovieCandidate(0, "Fight Club", null, 8340) };
        var config = MakeConfig(enabled: false);

        var results = MovieEnricher.Resolve(_fightClub, [], candidates, config);

        Assert.Empty(results);
    }

    [Fact]
    public void Custom_title_threshold_accepts_weaker_match()
    {
        var candidates = new[] { new MovieCandidate(0, "FC", null, 8340) };

        var defaultResults = MovieEnricher.Resolve(_fightClub, [], candidates);
        var config = MakeConfig(titleThreshold: 0.1f);
        var customResults = MovieEnricher.Resolve(_fightClub, [], candidates, config);

        Assert.Empty(defaultResults);
        Assert.Single(customResults);
    }

    [Fact]
    public void Suffix_spielfilm_country_year_matches_via_prefix()
    {
        var movie = new TmdbMovie(
            Id: 1740919, Title: "Schachnovelle", OriginalTitle: "Schachnovelle",
            ReleaseDate: "2021-08-19", Runtime: 112, ImdbId: "tt9781494");
        var candidates = new[]
        {
            new MovieCandidate(0, "Schachnovelle - Spielfilm, Deutschland/Österreich 2020", null, 6084)
        };

        var results = MovieEnricher.Resolve(movie, [], candidates);

        var item = Assert.Single(results);
        Assert.Equal(0.95f, item.Confidence);
        Assert.Equal(2021, item.Year);
    }

    [Fact]
    public void Suffix_parenthesized_genre_matches_via_prefix()
    {
        var candidates = new[] { new MovieCandidate(0, "Fight Club (Spielfilm)", null, 8340) };

        var results = MovieEnricher.Resolve(_fightClub, [], candidates);

        var item = Assert.Single(results);
        Assert.Equal(0.95f, item.Confidence);
        Assert.Equal(1999, item.Year);
    }

    [Fact]
    public void Title_without_suffix_still_uses_levenshtein()
    {
        var candidates = new[] { new MovieCandidate(0, "Fight Club", null, 8340) };

        var results = MovieEnricher.Resolve(_fightClub, [], candidates);

        var item = Assert.Single(results);
        Assert.True(item.Confidence > 0.95f);
    }

    [Fact]
    public void Suffix_only_title_does_not_match_unrelated_movie()
    {
        var candidates = new[] { new MovieCandidate(0, "Spielfilm", null, 5400) };

        var results = MovieEnricher.Resolve(_fightClub, [], candidates);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("Schachnovelle - Spielfilm, Deutschland/Österreich 2020", "Schachnovelle")]
    [InlineData("Domino - Spielfilm USA 2023", "Domino")]
    [InlineData("Emily (Spielfilm)", "Emily")]
    [InlineData("Code 3", "Code 3")]
    [InlineData("Title - Part 2 - Spielfilm", "Title")]
    public void StripSuffix_removes_genre_suffix(string input, string expected)
    {
        Assert.Equal(expected, MovieEnricher.StripSuffix(input));
    }

    private static EnrichmentConfig MakeConfig(
        bool enabled = true,
        float titleThreshold = 0.5f) =>
        new(enabled,
            [EnrichmentMethod.Title, EnrichmentMethod.Airdate],
            new TitleMatchConfig(titleThreshold),
            new AirdateMatchConfig(7),
            new RuntimeMatchConfig(0.35f, RuntimeMode.Tiebreaker),
            new YearMatchConfig(1));
}
