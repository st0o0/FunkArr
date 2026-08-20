using FunkArr.Search;
using FunkArr.Shared.Models;

namespace FunkArr.Tests.Search;

public class MatchingPipelineTests
{
    [Theory]
    [InlineData("Über den Dächern", "ueber den daechern")]
    [InlineData("Große Straße", "grosse strasse")]
    [InlineData("TATORT", "tatort")]
    [InlineData("Müller-Lüdenscheid", "mueller-luedenscheid")]
    public void NormalizeTitle_HandlesGermanCharacters(string input, string expected)
    {
        var result = MatchingPipeline.NormalizeTitle(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Show S01E03 Title", 1, 3)]
    [InlineData("Show s02e15 Something", 2, 15)]
    [InlineData("S10E01", 10, 1)]
    public void ExtractSeasonEpisode_FindsPattern(string title, int expectedSeason, int expectedEpisode)
    {
        var result = MatchingPipeline.ExtractSeasonEpisode(title);

        Assert.NotNull(result);
        Assert.Equal(expectedSeason, result.Value.season);
        Assert.Equal(expectedEpisode, result.Value.episode);
    }

    [Theory]
    [InlineData("No pattern here")]
    [InlineData("Episode 5")]
    [InlineData("Season 1")]
    public void ExtractSeasonEpisode_ReturnsNull_WhenNoPattern(string title)
    {
        var result = MatchingPipeline.ExtractSeasonEpisode(title);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("Tatort: Blutige Spur", "Tatort", true)]
    [InlineData("Der Tatort vom Sonntag", "Tatort", true)]
    [InlineData("Polizeiruf 110", "Tatort", false)]
    public void MatchesTitle_ComparesNormalized(string candidate, string expected, bool shouldMatch)
    {
        var result = MatchingPipeline.MatchesTitle(candidate, expected);
        Assert.Equal(shouldMatch, result);
    }

    [Fact]
    public void FilterResults_SkipsAudiodeskription()
    {
        var items = new[]
        {
            CreateItem("Tatort", "Normal Episode", 2700, "http://video.mp4"),
            CreateItem("Tatort", "Audiodeskription", 2700, "http://video-ad.mp4"),
        };

        var results = MatchingPipeline.FilterResults(items);

        Assert.Single(results);
        Assert.Equal("Normal Episode", results[0].Title);
    }

    [Fact]
    public void FilterResults_SkipsTrailer()
    {
        var items = new[]
        {
            CreateItem("Show", "Episode 1", 2700, "http://video.mp4"),
            CreateItem("Show", "Trailer zur neuen Staffel", 120, "http://trailer.mp4"),
        };

        var results = MatchingPipeline.FilterResults(items);

        Assert.Single(results);
    }

    [Fact]
    public void FilterResults_FiltersByDuration()
    {
        var items = new[]
        {
            CreateItem("Show", "Correct Duration", 2700, "http://correct.mp4"),
            CreateItem("Show", "Wrong Duration", 300, "http://wrong.mp4"),
        };

        var results = MatchingPipeline.FilterResults(items, expectedDurationSeconds: 2700);

        Assert.Single(results);
        Assert.Equal("Correct Duration", results[0].Title);
    }

    [Fact]
    public void FilterResults_AcceptsDurationWithinThreshold()
    {
        var items = new[]
        {
            CreateItem("Show", "Slightly Short", 2400, "http://video.mp4"),
        };

        var results = MatchingPipeline.FilterResults(items, expectedDurationSeconds: 2700);

        Assert.Single(results);
    }

    [Fact]
    public void FilterResults_GeneratesQualityVariants()
    {
        var items = new[]
        {
            new MediathekResultItem
            {
                Channel = "ARD",
                Topic = "Tatort",
                Title = "Episode 1",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Duration = 2700,
                Url_Video = "http://video.mp4",
                Url_Video_HD = "http://video_hd.mp4",
                Url_Video_Low = "http://video_low.mp4",
            },
        };

        var results = MatchingPipeline.FilterResults(items);

        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.Quality == QualityTier.HD1080);
        Assert.Contains(results, r => r.Quality == QualityTier.HD720);
        Assert.Contains(results, r => r.Quality == QualityTier.SD);
    }

    [Fact]
    public void MatchesShow_ReturnsTrueWhenShowNameIsNull()
    {
        var item = CreateItem("Tatort", "Episode", 2700, "http://v.mp4");
        var context = new MatchContext();

        Assert.True(MatchingPipeline.MatchesShow(item, context));
    }

    [Fact]
    public void MatchesShow_MatchesTopicField()
    {
        var item = CreateItem("Tatort", "Blutige Spur", 2700, "http://v.mp4");
        var context = new MatchContext { ShowName = "Tatort" };

        Assert.True(MatchingPipeline.MatchesShow(item, context));
    }

    [Fact]
    public void MatchesShow_RejectsNonMatchingTopic()
    {
        var item = CreateItem("Polizeiruf 110", "Episode", 2700, "http://v.mp4");
        var context = new MatchContext { ShowName = "Tatort" };

        Assert.False(MatchingPipeline.MatchesShow(item, context));
    }

    [Fact]
    public void MatchesEpisode_SkipsFilterWhenNoEpisodeInfo()
    {
        var item = CreateItem("Tatort", "Episode", 2700, "http://v.mp4");
        var context = new MatchContext { ShowName = "Tatort" };

        Assert.True(MatchingPipeline.MatchesEpisode(item, context));
    }

    [Fact]
    public void MatchesEpisode_MatchesSxxExxPattern()
    {
        var item = CreateItem("Tatort", "S01E03 Blutige Spur", 2700, "http://v.mp4");
        var context = new MatchContext { ShowName = "Tatort", Season = 1, Episode = 3 };

        Assert.True(MatchingPipeline.MatchesEpisode(item, context));
    }

    [Fact]
    public void MatchesEpisode_RejectsWrongEpisode()
    {
        var item = CreateItem("Tatort", "S01E05 Andere Folge", 2700, "http://v.mp4");
        var context = new MatchContext { ShowName = "Tatort", Season = 1, Episode = 3 };

        Assert.False(MatchingPipeline.MatchesEpisode(item, context));
    }

    [Fact]
    public void MatchesEpisode_MatchesAirDate()
    {
        var item = new MediathekResultItem
        {
            Channel = "ARD",
            Topic = "Tatort",
            Title = "Blutige Spur",
            Description = "Erstausstrahlung am 15.03.2025",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Duration = 2700,
            Url_Video = "http://v.mp4",
        };
        var context = new MatchContext
        {
            ShowName = "Tatort",
            AirDate = new DateTimeOffset(2025, 3, 15, 0, 0, 0, TimeSpan.Zero),
        };

        Assert.True(MatchingPipeline.MatchesEpisode(item, context));
    }

    [Fact]
    public void ScoreResult_HigherQualityScoresHigher()
    {
        var context = new MatchContext();
        var hd = CreateSearchResult(QualityTier.HD1080);
        var sd = CreateSearchResult(QualityTier.SD);

        var hdScored = MatchingPipeline.ScoreResult(hd, context);
        var sdScored = MatchingPipeline.ScoreResult(sd, context);

        Assert.True(hdScored.Score > sdScored.Score);
    }

    [Fact]
    public void ScoreResult_ExactTitleMatchScoresHigher()
    {
        var context = new MatchContext { ShowName = "Tatort" };
        var exact = CreateSearchResult(QualityTier.HD720) with { Topic = "Tatort" };
        var partial = CreateSearchResult(QualityTier.HD720) with { Topic = "Tatort: Spur" };

        var exactScored = MatchingPipeline.ScoreResult(exact, context);
        var partialScored = MatchingPipeline.ScoreResult(partial, context);

        Assert.True(exactScored.Score > partialScored.Score);
    }

    [Fact]
    public void Execute_ComposesFullPipeline()
    {
        var items = new[]
        {
            CreateItem("Tatort", "Blutige Spur", 2700, "http://video.mp4"),
            CreateItem("Tatort", "Audiodeskription", 2700, "http://ad.mp4"),
            CreateItem("Polizeiruf", "Episode", 2700, "http://other.mp4"),
            CreateItem("Tatort", "Trailer", 120, "http://trailer.mp4"),
        };

        var context = new MatchContext { ShowName = "Tatort" };
        var results = MatchingPipeline.Execute(items, context);

        Assert.Single(results);
        Assert.Equal("Blutige Spur", results[0].Title);
    }

    [Fact]
    public void Execute_ReturnsEmptyForNoMatches()
    {
        var items = new[]
        {
            CreateItem("Polizeiruf", "Episode", 2700, "http://v.mp4"),
        };

        var context = new MatchContext { ShowName = "Tatort" };
        var results = MatchingPipeline.Execute(items, context);

        Assert.Empty(results);
    }

    [Fact]
    public void Execute_ResultsOrderedByScoreDescending()
    {
        var items = new[]
        {
            new MediathekResultItem
            {
                Channel = "ARD", Topic = "Tatort", Title = "Episode",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Duration = 2700,
                Url_Video = "http://sd.mp4",
                Url_Video_HD = "http://hd.mp4",
            },
        };

        var context = new MatchContext { ShowName = "Tatort" };
        var results = MatchingPipeline.Execute(items, context);

        Assert.Equal(2, results.Count);
        Assert.True(results[0].Score >= results[1].Score);
        Assert.Equal(QualityTier.HD1080, results[0].Quality);
    }

    private static MediathekResultItem CreateItem(
        string topic, string title, int duration, string videoUrl) =>
        new()
        {
            Channel = "ARD",
            Topic = topic,
            Title = title,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Duration = duration,
            Url_Video = videoUrl,
        };

    private static SearchResult CreateSearchResult(QualityTier quality) =>
        new()
        {
            Title = "Episode",
            Topic = "Show",
            Channel = "ARD",
            Url = "http://v.mp4",
            DurationSeconds = 2700,
            SizeBytes = 1_000_000,
            Timestamp = DateTimeOffset.UtcNow,
            Quality = quality,
        };
}
