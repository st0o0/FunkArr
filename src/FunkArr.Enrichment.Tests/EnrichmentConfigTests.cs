using FunkArr.Enrichment.Tmdb;
using FunkArr.Enrichment.Tvdb;
using FunkArr.Messages.Enrichment;

namespace FunkArr.Enrichment.Tests;

public sealed class EnrichmentConfigTests
{
    private static readonly TvdbEpisode[] _episodes =
    [
        new(2026, 1, "Short Episode", "2026-01-01", 30),
        new(2026, 2, "Long Episode", "2026-01-08", 90),
        new(2026, 3, "Medium Episode", "2026-01-15", 60)
    ];

    [Fact]
    public void Runtime_filter_mode_excludes_non_matching_episodes()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Short Episode", null, null, 1800, null, null)
        };
        var config = MakeConfig(runtimeMode: RuntimeMode.Filter, runtimeTolerance: 0.2f);

        var results = EpisodeEnricher.Resolve(_episodes, candidates, config);

        var result = Assert.Single(results);
        Assert.Equal("1", result.Episode);
    }

    [Fact]
    public void Runtime_filter_no_match_falls_back_to_unfiltered()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Short Episode", null, null, 7200, null, null)
        };
        var config = MakeConfig(runtimeMode: RuntimeMode.Filter, runtimeTolerance: 0.05f);

        var results = EpisodeEnricher.Resolve(_episodes, candidates, config);

        var result = Assert.Single(results);
        Assert.Equal("1", result.Episode);
    }

    [Fact]
    public void Null_config_uses_defaults_episode_enricher()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Short Episode", null, null, 1800, null, null)
        };

        var results = EpisodeEnricher.Resolve(_episodes, candidates, config: null);

        var result = Assert.Single(results);
        Assert.Equal(MatchMethod.TitleMatch, result.Method);
    }

    [Fact]
    public void Null_config_uses_defaults_movie_enricher()
    {
        var movie = new TmdbMovie(550, "Fight Club", "Fight Club", "1999-10-15", 139, "tt0137523");
        var candidates = new[] { new MovieCandidate(0, "Fight Club", null, 8340) };

        var results = MovieEnricher.Resolve(movie, [], candidates, config: null);

        var result = Assert.Single(results);
        Assert.Equal(MatchMethod.TitleMatch, result.Method);
    }

    [Fact]
    public void Title_only_method_does_not_try_airdate()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Unknown Title", null,
                new DateTimeOffset(2026, 1, 1, 20, 15, 0, TimeSpan.Zero), 5340, null, null)
        };
        var config = MakeConfig(methods: [EnrichmentMethod.Title]);

        var results = EpisodeEnricher.Resolve(_episodes, candidates, config);

        Assert.Empty(results);
    }

    [Fact]
    public void Airdate_only_method_skips_title()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Short Episode", null,
                new DateTimeOffset(2026, 1, 2, 20, 15, 0, TimeSpan.Zero), 1800, null, null)
        };
        var config = MakeConfig(methods: [EnrichmentMethod.Airdate], minTitleAffinity: 0f);

        var results = EpisodeEnricher.Resolve(_episodes, candidates, config);

        var result = Assert.Single(results);
        Assert.Equal(MatchMethod.AirdateMatch, result.Method);
    }

    [Fact]
    public void High_title_threshold_rejects_fuzzy_match()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Short Ep", null, null, 1800, null, null)
        };
        var config = MakeConfig(titleThreshold: 0.95f);

        var results = EpisodeEnricher.Resolve(_episodes, candidates, config);

        Assert.Empty(results);
    }

    [Fact]
    public void Low_title_threshold_accepts_fuzzy_match()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Short Ep", null, null, 1800, null, null)
        };
        var config = MakeConfig(titleThreshold: 0.3f);

        var results = EpisodeEnricher.Resolve(_episodes, candidates, config);

        Assert.Single(results);
    }

    private static EnrichmentConfig MakeConfig(
        bool enabled = true,
        EnrichmentMethod[]? methods = null,
        float titleThreshold = 0.7f,
        int airdateTolerance = 7,
        float runtimeTolerance = 0.35f,
        RuntimeMode runtimeMode = RuntimeMode.Tiebreaker,
        float minTitleAffinity = 0.3f) =>
        new(enabled,
            methods ?? [EnrichmentMethod.Title, EnrichmentMethod.Airdate],
            new TitleMatchConfig(titleThreshold),
            new AirdateMatchConfig(airdateTolerance, minTitleAffinity),
            new RuntimeMatchConfig(runtimeTolerance, runtimeMode),
            new YearMatchConfig(1));
}
