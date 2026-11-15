using FunkArr.Core.Data;

namespace FunkArr.Download.Tests;

public sealed class SanitizeFileNameTests
{
    [Fact]
    public void Clean_title_passes_through_unchanged()
    {
        var result = DataPaths.SanitizeFileName("Tatort.S2026E01.GERMAN.1080p.WEB.h264");

        Assert.Equal("Tatort.S2026E01.GERMAN.1080p.WEB.h264", result);
    }

    [Fact]
    public void Replaces_forward_slash_with_underscore()
    {
        var result = DataPaths.SanitizeFileName("Show/Season/Episode");

        Assert.Equal("Show_Season_Episode", result);
    }

    [Fact]
    public void Replaces_backslash_with_underscore()
    {
        var result = DataPaths.SanitizeFileName("Title\\Subtitle");

        Assert.Equal("Title_Subtitle", result);
    }

    [Fact]
    public void Replaces_colon_with_underscore()
    {
        var result = DataPaths.SanitizeFileName("Tatort: Schwarzer Freitag");

        Assert.Equal("Tatort_ Schwarzer Freitag", result);
    }

    [Fact]
    public void Replaces_null_byte_with_underscore()
    {
        var result = DataPaths.SanitizeFileName("Title\0Injection");

        Assert.Equal("Title_Injection", result);
    }

    [Fact]
    public void Replaces_traversal_sequences()
    {
        var result = DataPaths.SanitizeFileName("Title/../../../secret");

        Assert.DoesNotContain("..", result);
    }

    [Fact]
    public void Path_traversal_attack_is_neutralized()
    {
        var result = DataPaths.SanitizeFileName("../../etc/passwd");

        Assert.DoesNotContain("..", result);
        Assert.DoesNotContain("/", result);
    }

    [Fact]
    public void Strips_leading_dots()
    {
        var result = DataPaths.SanitizeFileName(".hidden");

        Assert.Equal("hidden", result);
    }

    [Fact]
    public void Strips_trailing_dots()
    {
        var result = DataPaths.SanitizeFileName("Title...");

        Assert.Equal("Title", result);
    }

    [Fact]
    public void Strips_leading_and_trailing_spaces()
    {
        var result = DataPaths.SanitizeFileName("  Title  ");

        Assert.Equal("Title", result);
    }

    [Fact]
    public void Collapses_consecutive_underscores()
    {
        var result = DataPaths.SanitizeFileName("Title::/Subtitle");

        Assert.Equal("Title_Subtitle", result);
    }

    [Fact]
    public void Truncates_to_200_characters()
    {
        var longTitle = new string('A', 300);

        var result = DataPaths.SanitizeFileName(longTitle);

        Assert.Equal(200, result.Length);
    }

    [Fact]
    public void Empty_string_returns_unnamed()
    {
        var result = DataPaths.SanitizeFileName("");

        Assert.Equal("unnamed", result);
    }

    [Fact]
    public void All_invalid_characters_returns_unnamed()
    {
        var result = DataPaths.SanitizeFileName("///...");

        Assert.Equal("unnamed", result);
    }

    [Fact]
    public void Null_throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DataPaths.SanitizeFileName(null!));
    }

    [Theory]
    [InlineData("Tatort.S2026E01.GERMAN.1080p.WEB.h264-FunkArr", "Tatort.S2026E01.GERMAN.1080p.WEB.h264-FunkArr")]
    [InlineData("Normal Title With Spaces", "Normal Title With Spaces")]
    [InlineData("Title (2024)", "Title (2024)")]
    public void Normal_titles_pass_through(string input, string expected)
    {
        Assert.Equal(expected, DataPaths.SanitizeFileName(input));
    }
}
