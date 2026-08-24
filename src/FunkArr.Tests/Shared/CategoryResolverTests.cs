using FunkArr.Shared;

namespace FunkArr.Tests.Shared;

public class CategoryResolverTests
{
    [Fact]
    public void Resolve_NullCategory_ReturnsBasePath()
    {
        var result = CategoryResolver.Resolve("/downloads", null, new Dictionary<string, string>());
        Assert.Equal("/downloads", result);
    }

    [Fact]
    public void Resolve_EmptyCategory_ReturnsBasePath()
    {
        var result = CategoryResolver.Resolve("/downloads", "", new Dictionary<string, string>());
        Assert.Equal("/downloads", result);
    }

    [Fact]
    public void Resolve_AbsolutePathOverride_ReturnsConfiguredPath()
    {
        var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["movies"] = "/data/movies/incoming"
        };

        var result = CategoryResolver.Resolve("/downloads", "movies", config);
        Assert.Equal("/data/movies/incoming", result);
    }

    [Fact]
    public void Resolve_RelativePathOverride_CombinesWithBasePath()
    {
        var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tv"] = "serien"
        };

        var result = CategoryResolver.Resolve("/downloads/complete", "tv", config);
        Assert.Equal(Path.Combine("/downloads/complete", "serien"), result);
    }

    [Fact]
    public void Resolve_UnknownCategory_UsesCategoryAsSubfolder()
    {
        var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var result = CategoryResolver.Resolve("/downloads/complete", "anime", config);
        Assert.Equal(Path.Combine("/downloads/complete", "anime"), result);
    }

    [Fact]
    public void Resolve_CaseInsensitiveLookup_MatchesEntry()
    {
        var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tv"] = "serien"
        };

        var result = CategoryResolver.Resolve("/downloads", "TV", config);
        Assert.Equal(Path.Combine("/downloads", "serien"), result);
    }

    [Fact]
    public void Resolve_InvalidCharactersInCategory_SanitizesSubfolder()
    {
        var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var result = CategoryResolver.Resolve("/downloads", "tv/shows", config);
        Assert.Equal(Path.Combine("/downloads", "tvshows"), result);
    }

    [Fact]
    public void Resolve_CategoryAllInvalidChars_ReturnsFallbackName()
    {
        var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var result = CategoryResolver.Resolve("/downloads", "///", config);
        Assert.Equal(Path.Combine("/downloads", "_"), result);
    }
}
