using System.Text;
using FunkArr.ArrApi.Newznab;
using FunkArr.ArrApi.Newznab.Models;
using FunkArr.ArrApi.Newznab.Services;
using FunkArr.Core;
using FunkArr.Messages;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class NzbServiceTests
{
    [Fact]
    public void GetNzb_returns_success_for_valid_id()
    {
        var id = NzbTestData.Id("Test Title", "https://example.com/video.mp4", null, "ARD", 3600, 1000, MediaType.Show);

        var result = NzbService.GetNzb(id, TimeProvider.System);

        var success = Assert.IsType<NzbGetResult.Success>(result);
        Assert.NotEmpty(success.Content);
        Assert.Contains("funkarr-", success.FileName);
    }

    [Fact]
    public void GetNzb_returns_error_for_null_id()
    {
        var result = NzbService.GetNzb(null, TimeProvider.System);

        var error = Assert.IsType<NzbGetResult.Error>(result);
        Assert.Equal(200, error.ErrorDetail.Code);
    }

    [Fact]
    public void GetNzb_returns_error_for_empty_id()
    {
        var result = NzbService.GetNzb("", TimeProvider.System);

        var error = Assert.IsType<NzbGetResult.Error>(result);
        Assert.Equal(200, error.ErrorDetail.Code);
    }

    [Fact]
    public void GetNzb_returns_error_for_invalid_base64()
    {
        var result = NzbService.GetNzb("not-valid-base64!!!", TimeProvider.System);

        var error = Assert.IsType<NzbGetResult.Error>(result);
        Assert.Equal(201, error.ErrorDetail.Code);
    }

    [Theory]
    [InlineData(ValidationRss.TvGuid)]
    [InlineData(ValidationRss.MovieGuid)]
    public void GetNzb_rejects_validation_item_ids(string id)
    {
        var result = NzbService.GetNzb(id, TimeProvider.System);

        var error = Assert.IsType<NzbGetResult.Error>(result);
        Assert.Equal(201, error.ErrorDetail.Code);
    }

    [Theory]
    [InlineData("title-only")]
    [InlineData("{\"title\":\"t\",\"channel\":\"c\",\"duration\":1,\"size\":1,\"category\":\"show\"}")]
    [InlineData("{\"title\":\"t\",\"url\":\"\",\"channel\":\"c\",\"duration\":1,\"size\":1,\"category\":\"show\"}")]
    public void GetNzb_returns_incorrect_parameter_for_unusable_payload(string content)
    {
        var id = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));

        var result = NzbService.GetNzb(id, TimeProvider.System);

        var error = Assert.IsType<NzbGetResult.Error>(result);
        Assert.Equal(201, error.ErrorDetail.Code);
    }

    [Fact]
    public void GetNzb_includes_subtitle_url_meta()
    {
        var id = NzbTestData.Id("Title", "https://example.com/v.mp4", "https://example.com/sub.vtt", "ARD", 3600, 1000, MediaType.Show);

        var result = NzbService.GetNzb(id, TimeProvider.System);
        var success = Assert.IsType<NzbGetResult.Success>(result);
        var xml = Encoding.UTF8.GetString(success.Content);

        Assert.Contains($"<meta type=\"{FunkArrHeaders.SubtitleUrl}\">https://example.com/sub.vtt</meta>", xml);
    }

    [Theory]
    [InlineData(MediaType.Movie, "movie")]
    [InlineData(MediaType.Show, "tv")]
    public void GetNzb_includes_category_meta(MediaType category, string expected)
    {
        var id = NzbTestData.Id("Title", "https://example.com/v.mp4", null, "ARD", 3600, 1000, category);

        var result = NzbService.GetNzb(id, TimeProvider.System);
        var success = Assert.IsType<NzbGetResult.Success>(result);
        var xml = Encoding.UTF8.GetString(success.Content);

        Assert.Contains($"<meta type=\"{FunkArrHeaders.Category}\">{expected}</meta>", xml);
    }

    [Fact]
    public void ParseNzb_reads_payload_from_generated_nzb()
    {
        var payload = new NzbPayload(
            "Test Title", "https://example.com/v.mp4", "https://example.com/sub.vtt", "ARD", 3600, 1_000_000, MediaType.Movie);
        var generated = NzbService.GetNzb(payload.Encode(), TimeProvider.System);
        var success = Assert.IsType<NzbGetResult.Success>(generated);

        using var stream = new MemoryStream(success.Content);
        var parsed = NzbService.ParseNzb(stream);

        Assert.Equal(payload, parsed);
    }

    [Fact]
    public void ParseNzb_uses_defaults_for_missing_metas()
    {
        var nzbXml = NewznabXmlResult.Serialize(new Nzb());

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(nzbXml));
        var parsed = NzbService.ParseNzb(stream);

        Assert.Equal(new NzbPayload("Unknown", "", null, "", 0, 0, MediaType.Show), parsed);
    }

    [Fact]
    public void ParseNzb_ignores_legacy_url_meta()
    {
        var nzbXml = NewznabXmlResult.Serialize(new Nzb
        {
            Head = new NzbHead
            {
                Metas =
                [
                    new NzbMeta { Type = "title", Value = "Test Title" },
                    new NzbMeta { Type = "url", Value = "https://example.com/v.mp4" }
                ]
            }
        });

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(nzbXml));
        var parsed = NzbService.ParseNzb(stream);

        Assert.NotNull(parsed);
        Assert.Equal("", parsed.Url);
    }

    [Fact]
    public void ParseNzb_clears_non_http_url_meta()
    {
        var nzbXml = NewznabXmlResult.Serialize(new Nzb
        {
            Head = new NzbHead
            {
                Metas =
                [
                    new NzbMeta { Type = "title", Value = "Test Title" },
                    new NzbMeta { Type = FunkArrHeaders.Url, Value = "file:///etc/shadow" },
                    new NzbMeta { Type = FunkArrHeaders.Channel, Value = "ARD" }
                ]
            }
        });

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(nzbXml));
        var parsed = NzbService.ParseNzb(stream);

        Assert.NotNull(parsed);
        Assert.Equal("", parsed.Url);
    }

    [Fact]
    public void ParseNzb_clears_non_http_subtitle_url_meta()
    {
        var nzbXml = NewznabXmlResult.Serialize(new Nzb
        {
            Head = new NzbHead
            {
                Metas =
                [
                    new NzbMeta { Type = "title", Value = "Test Title" },
                    new NzbMeta { Type = FunkArrHeaders.Url, Value = "https://example.com/v.mp4" },
                    new NzbMeta { Type = FunkArrHeaders.SubtitleUrl, Value = "ftp://internal/sub.vtt" },
                    new NzbMeta { Type = FunkArrHeaders.Channel, Value = "ARD" }
                ]
            }
        });

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(nzbXml));
        var parsed = NzbService.ParseNzb(stream);

        Assert.NotNull(parsed);
        Assert.Equal("https://example.com/v.mp4", parsed.Url);
        Assert.Null(parsed.SubtitleUrl);
    }
}
