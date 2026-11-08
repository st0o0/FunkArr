namespace FunkArr.ArrApi.Tests;

public sealed class QueryStringRedactorTests
{
    [Theory]
    [InlineData("?t=tvsearch&apikey=secret123", "?t=tvsearch&apikey=[REDACTED]")]
    [InlineData("?apikey=secret123&t=tvsearch", "?apikey=[REDACTED]&t=tvsearch")]
    [InlineData("?t=search&q=Tatort&apikey=abc&limit=100", "?t=search&q=Tatort&apikey=[REDACTED]&limit=100")]
    [InlineData("?mode=queue", "?mode=queue")]
    [InlineData("?APIKEY=Secret", "?APIKEY=[REDACTED]")]
    [InlineData("?apikey=", "?apikey=[REDACTED]")]
    public void RedactApiKey_replaces_value(string input, string expected)
    {
        Assert.Equal(expected, QueryStringRedactor.RedactApiKey(input));
    }
}
