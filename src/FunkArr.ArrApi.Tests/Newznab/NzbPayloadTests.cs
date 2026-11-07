using System.Text;
using FunkArr.ArrApi.Newznab.Models;
using FunkArr.Messages;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class NzbPayloadTests
{
    [Fact]
    public void Encode_then_decode_returns_equal_payload()
    {
        var payload = new NzbPayload(
            "Tatort\tFolge", "https://example.com/video.mp4", "https://example.com/sub.vtt",
            "ARD", 5400, 1_234_567_890, MediaType.Show);

        var decoded = NzbPayload.Decode(payload.Encode());

        Assert.Equal(payload, decoded);
    }

    [Fact]
    public void Encode_then_decode_without_subtitle_keeps_null()
    {
        var payload = new NzbPayload("Film", "https://example.com/film.mp4", null, "ZDF", 100, 200, MediaType.Movie);

        var decoded = NzbPayload.Decode(payload.Encode());

        Assert.NotNull(decoded);
        Assert.Null(decoded.SubtitleUrl);
    }

    [Fact]
    public void Encode_then_decode_keeps_long_title()
    {
        var payload = new NzbPayload(new string('x', 2000), "https://example.com/v.mp4", null, "ARD", 1, 1, MediaType.Show);

        var decoded = NzbPayload.Decode(payload.Encode());

        Assert.Equal(payload, decoded);
    }

    [Fact]
    public void Encode_produces_expected_json()
    {
        var payload = new NzbPayload("T", "https://e.com/v", null, "ARD", 60, 10, MediaType.Movie);

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload.Encode()));

        Assert.Equal(
            "{\"title\":\"T\",\"url\":\"https://e.com/v\",\"channel\":\"ARD\",\"duration\":60,\"size\":10,\"category\":\"movie\"}",
            json);
    }

    [Theory]
    [InlineData("not-valid-base64!!!")]
    [InlineData("bm90IGpzb24=")]
    [InlineData("")]
    public void Decode_returns_null_for_invalid_input(string id) =>
        Assert.Null(NzbPayload.Decode(id));

    [Theory]
    [InlineData("{\"title\":\"t\",\"channel\":\"c\",\"duration\":1,\"size\":1,\"category\":\"show\"}")]
    [InlineData("{\"title\":\"t\",\"url\":\"\",\"channel\":\"c\",\"duration\":1,\"size\":1,\"category\":\"show\"}")]
    [InlineData("{\"url\":\"https://e.com/v\",\"channel\":\"c\",\"duration\":1,\"size\":1,\"category\":\"show\"}")]
    [InlineData("{\"title\":\"t\",\"url\":\"https://e.com/v\",\"duration\":1,\"size\":1,\"category\":\"show\"}")]
    [InlineData("{\"title\":\"t\",\"url\":\"https://e.com/v\",\"channel\":\"c\",\"duration\":1,\"size\":1,\"category\":\"bogus\"}")]
    [InlineData("{\"title\":\"t\",\"url\":\"https://e.com/v\",\"channel\":\"c\",\"duration\":1,\"size\":1,\"category\":1}")]
    public void Decode_returns_null_for_incomplete_payload(string json)
    {
        var id = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        Assert.Null(NzbPayload.Decode(id));
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://internal/file")]
    [InlineData("gopher://example.com/resource")]
    public void Decode_returns_null_for_non_http_url(string maliciousUrl)
    {
        var json = $"{{\"title\":\"t\",\"url\":\"{maliciousUrl}\",\"channel\":\"c\",\"duration\":1,\"size\":1,\"category\":\"show\"}}";
        var id = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        Assert.Null(NzbPayload.Decode(id));
    }

    [Fact]
    public void Decode_clears_non_http_subtitle_url_but_preserves_payload()
    {
        var json = "{\"title\":\"t\",\"url\":\"https://e.com/v\",\"subtitleUrl\":\"file:///etc/shadow\",\"channel\":\"c\",\"duration\":1,\"size\":1,\"category\":\"show\"}";
        var id = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        var decoded = NzbPayload.Decode(id);

        Assert.NotNull(decoded);
        Assert.Equal("https://e.com/v", decoded.Url);
        Assert.Null(decoded.SubtitleUrl);
    }
}
