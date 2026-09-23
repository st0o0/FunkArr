using System.Text;
using FunkArr.ArrApi.Newznab;
using FunkArr.ArrApi.Newznab.Models;
using FunkArr.Core;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class NzbServiceTests
{
    private readonly NzbService _service = new();

    [Fact]
    public void GetNzb_returns_success_for_valid_id()
    {
        var id = EncodeNzbId("Test Title", "https://example.com/video.mp4");

        var result = NzbService.GetNzb(id);

        var success = Assert.IsType<NzbGetResult.Success>(result);
        Assert.NotEmpty(success.Content);
        Assert.Contains("funkarr-", success.FileName);
    }

    [Fact]
    public void GetNzb_returns_error_for_null_id()
    {
        var result = NzbService.GetNzb(null);

        var error = Assert.IsType<NzbGetResult.Error>(result);
        Assert.Equal(200, error.ErrorDetail.Code);
    }

    [Fact]
    public void GetNzb_returns_error_for_empty_id()
    {
        var result = NzbService.GetNzb("");

        Assert.IsType<NzbGetResult.Error>(result);
    }

    [Fact]
    public void GetNzb_returns_error_for_invalid_base64()
    {
        var result = NzbService.GetNzb("not-valid-base64!!!");

        var error = Assert.IsType<NzbGetResult.Error>(result);
        Assert.Equal(201, error.ErrorDetail.Code);
    }

    [Fact]
    public void GetNzb_returns_error_for_missing_url()
    {
        var id = Convert.ToBase64String(Encoding.UTF8.GetBytes("title-only"));

        var result = NzbService.GetNzb(id);

        Assert.IsType<NzbGetResult.Error>(result);
    }

    [Fact]
    public void GetNzb_returns_error_for_empty_url()
    {
        var id = Convert.ToBase64String(Encoding.UTF8.GetBytes("title\t"));

        var result = NzbService.GetNzb(id);

        Assert.IsType<NzbGetResult.Error>(result);
    }

    [Fact]
    public void GetNzb_includes_subtitle_url_meta()
    {
        var id = EncodeNzbId("Title", "https://example.com/v.mp4", "https://example.com/sub.vtt");

        var result = NzbService.GetNzb(id);
        var success = Assert.IsType<NzbGetResult.Success>(result);
        var xml = Encoding.UTF8.GetString(success.Content);

        Assert.Contains(FunkArrHeaders.SubtitleUrl, xml);
        Assert.Contains("https://example.com/sub.vtt", xml);
    }

    [Fact]
    public void GetNzb_includes_category_meta()
    {
        var id = EncodeNzbId("Title", "https://example.com/v.mp4", category: "movie");

        var result = NzbService.GetNzb(id);
        var success = Assert.IsType<NzbGetResult.Success>(result);
        var xml = Encoding.UTF8.GetString(success.Content);

        Assert.Contains(FunkArrHeaders.Category, xml);
        Assert.Contains("movie", xml);
    }

    [Fact]
    public void ParseNzb_returns_result_for_valid_nzb()
    {
        var nzbXml = NewznabXmlResult.Serialize(new Nzb
        {
            Head = new NzbHead
            {
                Metas =
                [
                    new NzbMeta { Type = "title", Value = "Test Title" },
                    new NzbMeta { Type = FunkArrHeaders.Url, Value = "https://example.com/v.mp4" },
                ],
            },
        });

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(nzbXml));
        var result = NzbService.ParseNzb(stream);

        Assert.NotNull(result);
        Assert.Equal("Test Title", result.Meta("title"));
        Assert.Equal("https://example.com/v.mp4", result.Meta(FunkArrHeaders.Url));
    }

    [Fact]
    public void ParseNzb_returns_null_for_missing_meta()
    {
        var nzbXml = NewznabXmlResult.Serialize(new Nzb());

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(nzbXml));
        var result = NzbService.ParseNzb(stream);

        Assert.NotNull(result);
        Assert.Null(result.Meta("nonexistent"));
    }

    private static string EncodeNzbId(string title, string url, string? subtitleUrl = null, string? category = null)
    {
        var payload = string.Join('\t', title, url, subtitleUrl ?? "", "ARD", "3600", "1000", category ?? "");
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
    }
}
