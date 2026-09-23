using System.Text;
using FunkArr.ArrApi.Newznab;
using FunkArr.Core;
using Microsoft.AspNetCore.Mvc;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class NzbServiceTests
{
    private readonly NzbService _service = new();

    [Fact]
    public void GetNzb_returns_file_for_valid_id()
    {
        var id = EncodeNzbId("Test Title", "https://example.com/video.mp4");

        var result = _service.GetNzb(id);

        Assert.IsType<FileContentResult>(result);
        var file = (FileContentResult)result;
        Assert.Equal("application/x-nzb", file.ContentType);
        Assert.Contains("funkarr-", file.FileDownloadName);
    }

    [Fact]
    public void GetNzb_returns_error_for_null_id()
    {
        var result = _service.GetNzb(null);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(400, content.StatusCode);
    }

    [Fact]
    public void GetNzb_returns_error_for_empty_id()
    {
        var result = _service.GetNzb("");

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(400, content.StatusCode);
    }

    [Fact]
    public void GetNzb_returns_error_for_invalid_base64()
    {
        var result = _service.GetNzb("not-valid-base64!!!");

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(400, content.StatusCode);
    }

    [Fact]
    public void GetNzb_returns_error_for_missing_url()
    {
        var id = Convert.ToBase64String(Encoding.UTF8.GetBytes("title-only"));

        var result = _service.GetNzb(id);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(400, content.StatusCode);
    }

    [Fact]
    public void GetNzb_returns_error_for_empty_url()
    {
        var id = Convert.ToBase64String(Encoding.UTF8.GetBytes("title\t"));

        var result = _service.GetNzb(id);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(400, content.StatusCode);
    }

    [Fact]
    public void GetNzb_includes_subtitle_url_meta()
    {
        var id = EncodeNzbId("Title", "https://example.com/v.mp4", "https://example.com/sub.vtt");

        var result = _service.GetNzb(id);
        var file = Assert.IsType<FileContentResult>(result);
        var xml = Encoding.UTF8.GetString(file.FileContents);

        Assert.Contains(FunkArrHeaders.SubtitleUrl, xml);
        Assert.Contains("https://example.com/sub.vtt", xml);
    }

    [Fact]
    public void GetNzb_includes_category_meta()
    {
        var id = EncodeNzbId("Title", "https://example.com/v.mp4", category: "movie");

        var result = _service.GetNzb(id);
        var file = Assert.IsType<FileContentResult>(result);
        var xml = Encoding.UTF8.GetString(file.FileContents);

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
        var result = _service.ParseNzb(stream);

        Assert.NotNull(result);
        Assert.Equal("Test Title", result.Meta("title"));
        Assert.Equal("https://example.com/v.mp4", result.Meta(FunkArrHeaders.Url));
    }

    [Fact]
    public void ParseNzb_returns_null_for_missing_meta()
    {
        var nzbXml = NewznabXmlResult.Serialize(new Nzb());

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(nzbXml));
        var result = _service.ParseNzb(stream);

        Assert.NotNull(result);
        Assert.Null(result.Meta("nonexistent"));
    }

    private static string EncodeNzbId(string title, string url, string? subtitleUrl = null, string? category = null)
    {
        var payload = string.Join('\t', title, url, subtitleUrl ?? "", "ARD", "3600", "1000", category ?? "");
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
    }
}
