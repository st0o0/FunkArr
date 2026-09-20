using FunkArr.Messages.Enrichment;

namespace FunkArr.Enrichment.Tests;

public sealed class EpisodeEnricherTests
{
    private static readonly TvdbEpisode[] _tatortEpisodes =
    [
        new(2026, 1, "Nachtschatten", "2026-01-01", 89),
        new(2026, 5, "Wenn man nur einen retten könnte", "2026-01-25", 88),
        new(2026, 9, "Sashimi Spezial", "2026-03-01", 89),
        new(2026, 16, "Könige der Nacht", "2026-05-03", 88),
    ];

    [Fact]
    public void RegexExtracted_passes_through()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "ZDF Magazin Royale S2026E01", null, null, 1800,
                ExistingSeason: "2026", ExistingEpisode: "01"),
        };

        var results = EpisodeEnricher.Resolve(_tatortEpisodes, candidates);

        Assert.Single(results);
        Assert.Equal("2026", results[0].Season);
        Assert.Equal("01", results[0].Episode);
        Assert.Equal(1.0f, results[0].Confidence);
        Assert.Equal(MatchMethod.RegexExtracted, results[0].Method);
    }

    [Fact]
    public void TitleMatch_exact_match()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Nachtschatten", null, null, 5340, null, null),
        };

        var results = EpisodeEnricher.Resolve(_tatortEpisodes, candidates);

        Assert.Single(results);
        Assert.Equal("2026", results[0].Season);
        Assert.Equal("1", results[0].Episode);
        Assert.Equal("Nachtschatten", results[0].EpisodeName);
        Assert.True(results[0].Confidence >= 0.95f);
        Assert.Equal(MatchMethod.TitleMatch, results[0].Method);
    }

    [Fact]
    public void TitleMatch_uses_constructed_title()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Tatort: Nachtschatten", "Nachtschatten", null, 5340, null, null),
        };

        var results = EpisodeEnricher.Resolve(_tatortEpisodes, candidates);

        Assert.Single(results);
        Assert.Equal("Nachtschatten", results[0].EpisodeName);
    }

    [Fact]
    public void TitleMatch_fuzzy_match()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Könige der Nacht (2026)", null, null, 5280, null, null),
        };

        var results = EpisodeEnricher.Resolve(_tatortEpisodes, candidates);

        Assert.Single(results);
        Assert.Equal("Könige der Nacht", results[0].EpisodeName);
        Assert.Equal(MatchMethod.TitleMatch, results[0].Method);
    }

    [Fact]
    public void TitleMatch_below_threshold_no_match()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Completely Different Title", null, null, 5340, null, null),
        };

        var results = EpisodeEnricher.Resolve(_tatortEpisodes, candidates);

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

        var results = EpisodeEnricher.Resolve(_tatortEpisodes, candidates);

        Assert.Single(results);
        Assert.Equal("2026", results[0].Season);
        Assert.Equal("1", results[0].Episode);
        Assert.Equal(MatchMethod.AirdateMatch, results[0].Method);
    }

    [Fact]
    public void AirdateMatch_outside_tolerance_no_match()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Unknown Title", null,
                new DateTimeOffset(2026, 6, 15, 20, 15, 0, TimeSpan.Zero), 5340, null, null),
        };

        var results = EpisodeEnricher.Resolve(_tatortEpisodes, candidates);

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

        var results = EpisodeEnricher.Resolve(_tatortEpisodes, candidates);

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

        var results = EpisodeEnricher.Resolve(_tatortEpisodes, candidates);

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

        var results = EpisodeEnricher.Resolve(_tatortEpisodes, candidates);

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

        var results = EpisodeEnricher.Resolve(_tatortEpisodes, candidates);

        Assert.Single(results);
        Assert.Equal(1.0f, results[0].Confidence);
    }

    [Fact]
    public void Disabled_enrichment_returns_empty()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Nachtschatten", null, null, 5340, null, null),
        };
        var config = MakeConfig(enabled: false);

        var results = EpisodeEnricher.Resolve(_tatortEpisodes, candidates, config);

        Assert.Empty(results);
    }

    [Fact]
    public void Custom_title_threshold_accepts_weaker_match()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Nacht", null, null, 5340, null, null),
        };

        var defaultResults = EpisodeEnricher.Resolve(_tatortEpisodes, candidates);
        var config = MakeConfig(titleThreshold: 0.3f);
        var customResults = EpisodeEnricher.Resolve(_tatortEpisodes, candidates, config);

        Assert.Empty(defaultResults);
        Assert.Single(customResults);
        Assert.Equal(MatchMethod.TitleMatch, customResults[0].Method);
    }

    [Fact]
    public void Airdate_only_method_skips_title_matching()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Nachtschatten", null,
                new DateTimeOffset(2026, 1, 2, 20, 15, 0, TimeSpan.Zero), 5340, null, null),
        };
        var config = MakeConfig(methods: [EnrichmentMethod.Airdate]);

        var results = EpisodeEnricher.Resolve(_tatortEpisodes, candidates, config);

        Assert.Single(results);
        Assert.Equal(MatchMethod.AirdateMatch, results[0].Method);
    }

    [Fact]
    public void Custom_airdate_tolerance_narrows_window()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Unknown Title", null,
                new DateTimeOffset(2026, 1, 5, 20, 15, 0, TimeSpan.Zero), 5340, null, null),
        };

        var defaultResults = EpisodeEnricher.Resolve(_tatortEpisodes, candidates);
        var config = MakeConfig(airdateTolerance: 2);
        var narrowResults = EpisodeEnricher.Resolve(_tatortEpisodes, candidates, config);

        Assert.Single(defaultResults);
        Assert.Empty(narrowResults);
    }

    private static EnrichmentConfig MakeConfig(
        bool enabled = true,
        EnrichmentMethod[]? methods = null,
        float titleThreshold = 0.7f,
        int airdateTolerance = 7,
        float runtimeTolerance = 0.35f,
        RuntimeMode runtimeMode = RuntimeMode.Tiebreaker) =>
        new(enabled,
            methods ?? [EnrichmentMethod.Title, EnrichmentMethod.Airdate],
            new TitleMatchConfig(titleThreshold),
            new AirdateMatchConfig(airdateTolerance),
            new RuntimeMatchConfig(runtimeTolerance, runtimeMode),
            new YearMatchConfig(1));
}
