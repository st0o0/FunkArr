using FunkArr.Messages;
using FunkArr.Messages.Mediathek;

namespace FunkArr.Search.Tests;

public sealed class VariantDetectorTests
{
    [Theory]
    [InlineData("Herz aus Eis (Audiodeskription)", MediathekVariant.AudioDescription)]
    [InlineData("Herz aus Eis - mit Audio-Deskription", MediathekVariant.AudioDescription)]
    [InlineData("Herz aus Eis (Hörfassung)", MediathekVariant.AudioDescription)]
    [InlineData("Herz aus Eis (AD)", MediathekVariant.AudioDescription)]
    [InlineData("Herz aus Eis ( ad )", MediathekVariant.AudioDescription)]
    [InlineData("Herz aus Eis mit AD", MediathekVariant.AudioDescription)]
    [InlineData("Herz aus Eis (mit Gebärdensprache)", MediathekVariant.SignLanguage)]
    [InlineData("Herz aus Eis (DGS)", MediathekVariant.SignLanguage)]
    [InlineData("Nachrichten in Einfacher Sprache", MediathekVariant.PlainLanguage)]
    [InlineData("logo! in Leichte Sprache", MediathekVariant.PlainLanguage)]
    [InlineData("Tagesschau in Klarer Sprache", MediathekVariant.PlainLanguage)]
    [InlineData("Tagesschau - Klare Sprache", MediathekVariant.PlainLanguage)]
    [InlineData("Die Brücke (Originalversion)", MediathekVariant.OriginalVersion)]
    [InlineData("Die Brücke (Originalfassung)", MediathekVariant.OriginalVersion)]
    [InlineData("Die Brücke - Original mit Untertiteln", MediathekVariant.OriginalVersion)]
    [InlineData("Die Brücke (OV)", MediathekVariant.OriginalVersion)]
    [InlineData("Die Brücke (OmU)", MediathekVariant.OriginalVersion)]
    [InlineData("Herz aus Eis", MediathekVariant.Standard)]
    [InlineData("Die ADAC-Story", MediathekVariant.Standard)]
    [InlineData("OVB Nachrichten", MediathekVariant.Standard)]
    [InlineData("Ein Leben mit Adele", MediathekVariant.Standard)]
    public void Detect_from_title(string title, MediathekVariant expected)
    {
        Assert.Equal(expected, VariantDetector.Detect(title, "Tatort"));
    }

    [Fact]
    public void Detect_from_topic_when_title_has_no_marker()
    {
        Assert.Equal(MediathekVariant.SignLanguage, VariantDetector.Detect("Herz aus Eis", "Tatort (mit Gebärdensprache)"));
    }

    [Fact]
    public void Detect_prefers_audio_description_over_original_version()
    {
        Assert.Equal(
            MediathekVariant.AudioDescription,
            VariantDetector.Detect("Die Brücke (Originalfassung, Audiodeskription)", "Die Brücke"));
    }

    [Fact]
    public void Detect_handles_missing_values()
    {
        Assert.Equal(MediathekVariant.Standard, VariantDetector.Detect(null, null));
    }

    [Fact]
    public void SourceInfo_from_ignores_description()
    {
        var item = new MediathekItem("ARD", "Tatort", "Herz aus Eis", "Auch mit Audiodeskription verfügbar",
            1719244800, 5400, 0, null, "https://v.mp4", null, null, null);

        Assert.Equal(MediathekVariant.Standard, SourceInfo.From(item).Variant);
    }

    [Fact]
    public void SourceInfo_from_detects_variant()
    {
        var item = new MediathekItem("ARD", "Tatort", "Herz aus Eis (Audiodeskription)", null,
            1719244800, 5400, 0, null, "https://v.mp4", null, null, null);

        Assert.Equal(MediathekVariant.AudioDescription, SourceInfo.From(item).Variant);
    }
}
