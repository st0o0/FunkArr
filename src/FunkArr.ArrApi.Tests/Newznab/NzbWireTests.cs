using System.Text;
using FunkArr.ArrApi.Newznab;
using Microsoft.Extensions.Time.Testing;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class NzbWireTests
{
    private static readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));

    [Fact]
    public void GetNzb_with_all_fields_produces_expected_xml()
    {
        var id = NzbTestData.Id(
            "Tatort", "https://example.com/video.mp4", "https://example.com/sub.vtt", "ARD", 5400, 1_234_567_890, "tv");

        var result = NzbService.GetNzb(id, _time);

        var success = Assert.IsType<NzbGetResult.Success>(result);
        Assert.Multiple(
            () => Assert.Equal("funkarr-2026-01-02_03-04-05.nzb", success.FileName),
            () => Assert.Equal(_expectedFullXml, Encoding.UTF8.GetString(success.Content)));
    }

    [Fact]
    public void GetNzb_without_subtitle_omits_subtitle_meta()
    {
        var id = NzbTestData.Id("Film", "https://example.com/film.mp4", null, "ZDF", 100, 200, "movie");

        var result = NzbService.GetNzb(id, _time);

        var xml = Encoding.UTF8.GetString(Assert.IsType<NzbGetResult.Success>(result).Content);
        Assert.Multiple(
            () => Assert.DoesNotContain("X-FunkArr-SubtitleUrl", xml),
            () => Assert.Contains("<meta type=\"X-FunkArr-Category\">movie</meta>", xml));
    }

    private const string _expectedFullXml =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?><nzb xmlns:newznab=\"http://www.newznab.com/DTD/2010/feeds/attributes/\" " +
        "xmlns=\"http://www.newzbin.com/DTD/2003/nzb\"><head><meta type=\"title\">Tatort</meta>" +
        "<meta type=\"X-FunkArr-Url\">https://example.com/video.mp4</meta><meta type=\"X-FunkArr-Channel\">ARD</meta>" +
        "<meta type=\"X-FunkArr-Duration\">5400</meta><meta type=\"X-FunkArr-Size\">1234567890</meta>" +
        "<meta type=\"X-FunkArr-SubtitleUrl\">https://example.com/sub.vtt</meta><meta type=\"X-FunkArr-Category\">tv</meta>" +
        "</head><file post_id=\"1\"><groups><group>a.b.mediathek</group></groups>" +
        "<segments><segment number=\"1\">FunkArr@news.example.com</segment></segments></file></nzb>";
}
