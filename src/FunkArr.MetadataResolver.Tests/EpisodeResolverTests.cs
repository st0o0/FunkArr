using FunkArr.Messages.MetadataResolver;

namespace FunkArr.MetadataResolver.Tests;

public sealed class EpisodeResolverTests
{
    private static readonly TvdbEpisode[] _tatortEpisodes =
    [
        new(2026, 1, "Nachtschatten", "2026-01-01", 89),
        new(2026, 5, "Wenn man nur einen retten könnte", "2026-01-25", 88),
        new(2026, 9, "Sashimi Spezial", "2026-03-01", 89),
        new(2026, 16, "Könige der Nacht", "2026-05-03", 88),
    ];

    private static readonly ResolutionConfig _fuzzy = new("fuzzy", 0.7f, 7);
    private static readonly ResolutionConfig _strict = new("strict", 0.95f, 7);
    private static readonly ResolutionConfig _none = new("none");

    [Fact]
    public void RegexExtracted_passes_through()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "ZDF Magazin Royale S2026E01", null, null, 1800,
                ExistingSeason: "2026", ExistingEpisode: "01"),
        };

        var results = EpisodeResolver.Resolve(_tatortEpisodes, candidates, _fuzzy);

        Assert.Single(results);
        Assert.Equal("2026", results[0].Season);
        Assert.Equal("01", results[0].Episode);
        Assert.Equal(1.0f, results[0].Confidence);
        Assert.Equal("RegexExtracted", results[0].Strategy);
    }

    [Fact]
    public void FuzzyTitleMatch_exact_match()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Nachtschatten", null, null, 5340, null, null),
        };

        var results = EpisodeResolver.Resolve(_tatortEpisodes, candidates, _fuzzy);

        Assert.Single(results);
        Assert.Equal("2026", results[0].Season);
        Assert.Equal("1", results[0].Episode);
        Assert.Equal("Nachtschatten", results[0].EpisodeName);
        Assert.True(results[0].Confidence >= 0.95f);
        Assert.Equal("FuzzyTitleMatch", results[0].Strategy);
    }

    [Fact]
    public void FuzzyTitleMatch_uses_constructed_title()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Tatort: Nachtschatten", "Nachtschatten", null, 5340, null, null),
        };

        var results = EpisodeResolver.Resolve(_tatortEpisodes, candidates, _fuzzy);

        Assert.Single(results);
        Assert.Equal("Nachtschatten", results[0].EpisodeName);
    }

    [Fact]
    public void FuzzyTitleMatch_fuzzy_match()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Könige der Nacht (2026)", null, null, 5280, null, null),
        };

        var results = EpisodeResolver.Resolve(_tatortEpisodes, candidates, _fuzzy);

        Assert.Single(results);
        Assert.Equal("Könige der Nacht", results[0].EpisodeName);
        Assert.Equal("FuzzyTitleMatch", results[0].Strategy);
    }

    [Fact]
    public void FuzzyTitleMatch_below_threshold_no_match()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Completely Different Title", null, null, 5340, null, null),
        };

        var results = EpisodeResolver.Resolve(_tatortEpisodes, candidates, _fuzzy);

        Assert.Empty(results);
    }

    [Fact]
    public void AirdateMatch_within_tolerance()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Unknown Title", null,
                new DateTimeOffset(2026, 1, 2, 20, 15, 0, TimeSpan.Zero), 5340, null, null),
        };

        var results = EpisodeResolver.Resolve(_tatortEpisodes, candidates, _fuzzy);

        Assert.Single(results);
        Assert.Equal("2026", results[0].Season);
        Assert.Equal("1", results[0].Episode);
        Assert.Equal("AirdateMatch", results[0].Strategy);
    }

    [Fact]
    public void AirdateMatch_outside_tolerance_no_match()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Unknown Title", null,
                new DateTimeOffset(2026, 6, 15, 20, 15, 0, TimeSpan.Zero), 5340, null, null),
        };

        var results = EpisodeResolver.Resolve(_tatortEpisodes, candidates, _fuzzy);

        Assert.Empty(results);
    }

    [Fact]
    public void Strategy_none_returns_empty()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Nachtschatten", null, null, 5340, null, null),
        };

        var results = EpisodeResolver.Resolve(_tatortEpisodes, candidates, _none);

        Assert.Empty(results);
    }

    [Fact]
    public void Strategy_strict_uses_high_threshold()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Könige der Nacht (2026)", null, null, 5280, null, null),
        };

        var results = EpisodeResolver.Resolve(_tatortEpisodes, candidates, _strict);

        Assert.Empty(results);
    }

    [Fact]
    public void Unresolved_candidate_not_in_results()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Nachtschatten", null, null, 5340, null, null),
            new EpisodeCandidate(1, "Totally Unknown Episode", null, null, 5340, null, null),
        };

        var results = EpisodeResolver.Resolve(_tatortEpisodes, candidates, _fuzzy);

        Assert.Single(results);
        Assert.Equal(0, results[0].Index);
    }

    [Fact]
    public void Multiple_candidates_resolved_independently()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Nachtschatten", null, null, 5340, null, null),
            new EpisodeCandidate(1, "Sashimi Spezial", null, null, 5340, null, null),
        };

        var results = EpisodeResolver.Resolve(_tatortEpisodes, candidates, _fuzzy);

        Assert.Equal(2, results.Length);
        Assert.Equal("1", results[0].Episode);
        Assert.Equal("9", results[1].Episode);
    }

    [Fact]
    public void RegexExtracted_enriches_with_tvdb_name()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Something", null, null, 5340,
                ExistingSeason: "2026", ExistingEpisode: "1"),
        };

        var results = EpisodeResolver.Resolve(_tatortEpisodes, candidates, _fuzzy);

        Assert.Single(results);
        Assert.Equal("Nachtschatten", results[0].EpisodeName);
    }

    [Fact]
    public void Airdate_exact_match_has_high_confidence()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Unknown", null,
                new DateTimeOffset(2026, 1, 1, 20, 15, 0, TimeSpan.Zero), 5340, null, null),
        };

        var results = EpisodeResolver.Resolve(_tatortEpisodes, candidates, _fuzzy);

        Assert.Single(results);
        Assert.Equal(1.0f, results[0].Confidence);
    }
}
