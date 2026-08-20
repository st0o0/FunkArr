using FunkArr.Search;

namespace FunkArr.Tests.Search;

public class DateMatcherTests
{
    [Fact]
    public void MatchesAirDate_GermanDateFormat()
    {
        var result = DateMatcher.MatchesAirDate(
            "Sendung vom 15.01.2024",
            null,
            new DateTimeOffset(2024, 1, 15, 20, 15, 0, TimeSpan.FromHours(1)));

        Assert.True(result);
    }

    [Fact]
    public void MatchesAirDate_IsoDateFormat()
    {
        var result = DateMatcher.MatchesAirDate(
            "Broadcast 2024-01-15",
            null,
            new DateTimeOffset(2024, 1, 15, 20, 15, 0, TimeSpan.FromHours(1)));

        Assert.True(result);
    }

    [Fact]
    public void MatchesAirDate_WrongDate_ReturnsFalse()
    {
        var result = DateMatcher.MatchesAirDate(
            "Sendung vom 20.01.2024",
            null,
            new DateTimeOffset(2024, 1, 15, 20, 15, 0, TimeSpan.FromHours(1)));

        Assert.False(result);
    }

    [Fact]
    public void MatchesAirDate_NoDateInText_ReturnsTrue()
    {
        var result = DateMatcher.MatchesAirDate(
            "Tatort: Blutige Spur",
            null,
            new DateTimeOffset(2024, 1, 15, 20, 15, 0, TimeSpan.FromHours(1)));

        Assert.True(result);
    }

    [Fact]
    public void MatchesAirDate_DateInDescription()
    {
        var result = DateMatcher.MatchesAirDate(
            "Tatort",
            "Erstausstrahlung am 15.01.2024",
            new DateTimeOffset(2024, 1, 15, 20, 15, 0, TimeSpan.FromHours(1)));

        Assert.True(result);
    }
}
