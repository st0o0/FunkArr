using FunkArr.Api;

namespace FunkArr.Tests.Api;

public sealed class PathMappingHelperTests
{
    [Fact]
    public void ParsePathMapping_ValidMapping_ReturnsTuple()
    {
        var result = PathMappingHelper.ParsePathMapping("/container/path:/host/path");

        Assert.NotNull(result);
        Assert.Equal("/container/path", result.Value.From);
        Assert.Equal("/host/path", result.Value.To);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ParsePathMapping_NullOrEmpty_ReturnsNull(string? mapping)
    {
        var result = PathMappingHelper.ParsePathMapping(mapping);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("no-colon-here")]
    [InlineData("too:many:colons")]
    public void ParsePathMapping_InvalidFormat_ReturnsNull(string mapping)
    {
        var result = PathMappingHelper.ParsePathMapping(mapping);

        Assert.Null(result);
    }

    [Fact]
    public void MapPath_MatchingPrefix_ReplacesPrefixWithTo()
    {
        var mapping = ("/container", "/host");

        var result = PathMappingHelper.MapPath("/container/downloads/file.mkv", mapping);

        Assert.Equal("/host/downloads/file.mkv", result);
    }

    [Fact]
    public void MapPath_NonMatchingPrefix_ReturnsOriginal()
    {
        var mapping = ("/container", "/host");

        var result = PathMappingHelper.MapPath("/other/path/file.mkv", mapping);

        Assert.Equal("/other/path/file.mkv", result);
    }

    [Fact]
    public void MapPath_NullMapping_ReturnsOriginal()
    {
        var result = PathMappingHelper.MapPath("/some/path", null);

        Assert.Equal("/some/path", result);
    }

    [Fact]
    public void MapPath_EmptyPath_ReturnsEmpty()
    {
        var mapping = ("/container", "/host");

        var result = PathMappingHelper.MapPath("", mapping);

        Assert.Equal("", result);
    }

    [Fact]
    public void MapPath_CaseInsensitive_MatchesAndReplaces()
    {
        var mapping = ("/Container", "/host");

        var result = PathMappingHelper.MapPath("/container/file.mkv", mapping);

        Assert.Equal("/host/file.mkv", result);
    }
}
