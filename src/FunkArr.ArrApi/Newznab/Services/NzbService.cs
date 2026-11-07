using System.Text;
using System.Xml;
using System.Xml.Serialization;
using FunkArr.ArrApi.Newznab.Models;
using FunkArr.Core;
using FunkArr.Messages;

namespace FunkArr.ArrApi.Newznab.Services;

public static class NzbService
{
    private const string _titleMeta = "title";

    private static readonly XmlSerializer _nzbSerializer = new(typeof(Nzb));
    private static readonly XmlReaderSettings _xmlSettings = new() { DtdProcessing = DtdProcessing.Ignore };

    internal static NzbGetResult GetNzb(string? id, TimeProvider timeProvider)
    {
        if (string.IsNullOrEmpty(id))
        {
            return new NzbGetResult.Error(NewznabError.MissingParameter);
        }

        var payload = NzbPayload.Decode(id);
        if (payload is null)
        {
            return new NzbGetResult.Error(NewznabError.IncorrectParameter);
        }

        var xml = NewznabXmlResult.Serialize(ToNzb(payload));

        return new NzbGetResult.Success(
            Encoding.UTF8.GetBytes(xml),
            $"funkarr-{timeProvider.GetUtcNow().UtcDateTime:yyyy-MM-dd_HH-mm-ss}.nzb");
    }

    internal static NzbPayload? ParseNzb(Stream stream)
    {
        using var xmlReader = XmlReader.Create(stream, _xmlSettings);
        return _nzbSerializer.Deserialize(xmlReader) is Nzb nzb ? FromNzb(nzb) : null;
    }

    private static Nzb ToNzb(NzbPayload payload)
    {
        var metas = new List<NzbMeta>
        {
            new() { Type = _titleMeta, Value = payload.Title },
            new() { Type = FunkArrHeaders.Url, Value = payload.Url },
            new() { Type = FunkArrHeaders.Channel, Value = payload.Channel },
            new() { Type = FunkArrHeaders.Duration, Value = payload.Duration.ToString() },
            new() { Type = FunkArrHeaders.Size, Value = payload.Size.ToString() }
        };

        if (payload.SubtitleUrl is not null)
        {
            metas.Add(new NzbMeta { Type = FunkArrHeaders.SubtitleUrl, Value = payload.SubtitleUrl });
        }

        metas.Add(new NzbMeta { Type = FunkArrHeaders.Category, Value = ToCategoryMeta(payload.Category) });

        return new Nzb { Head = new NzbHead { Metas = metas } };
    }

    private static NzbPayload FromNzb(Nzb nzb)
    {
        string? Meta(string type) => nzb.Head.Metas.FirstOrDefault(m => m.Type == type)?.Value;

        _ = int.TryParse(Meta(FunkArrHeaders.Duration), out var duration);
        _ = long.TryParse(Meta(FunkArrHeaders.Size), out var size);

        var url = Meta(FunkArrHeaders.Url) ?? "";
        var subtitleUrl = Meta(FunkArrHeaders.SubtitleUrl);

        return new NzbPayload(
            Title: Meta(_titleMeta) ?? "Unknown",
            Url: UrlValidation.HasAllowedScheme(url) ? url : "",
            SubtitleUrl: UrlValidation.HasAllowedScheme(subtitleUrl) ? subtitleUrl : null,
            Channel: Meta(FunkArrHeaders.Channel) ?? "",
            Duration: duration,
            Size: size,
            Category: FromCategoryMeta(Meta(FunkArrHeaders.Category)));
    }

    private static string ToCategoryMeta(MediaType category) => category switch
    {
        MediaType.Show => "tv",
        MediaType.Movie => "movie",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
    };

    private static MediaType FromCategoryMeta(string? value) => value switch
    {
        "movie" or "movies" => MediaType.Movie,
        _ => MediaType.Show
    };
}
