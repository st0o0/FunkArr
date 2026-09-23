using System.Text;
using System.Xml;
using System.Xml.Serialization;
using FunkArr.ArrApi.Newznab.Models;
using FunkArr.Core;

namespace FunkArr.ArrApi.Newznab;

public sealed class NzbService
{
    private static readonly XmlSerializer _nzbSerializer = new(typeof(Nzb));
    private static readonly XmlReaderSettings _xmlSettings = new() { DtdProcessing = DtdProcessing.Ignore };

    internal NzbGetResult GetNzb(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return new NzbGetResult.Error(NewznabError.MissingParameter);

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(id));
        }
        catch (FormatException)
        {
            return new NzbGetResult.Error(NewznabError.IncorrectParameter);
        }

        var parts = decoded.Split('\t');
        if (parts.Length < 2)
            return new NzbGetResult.Error(NewznabError.IncorrectParameter);

        var title = parts[0];
        var url = parts[1];

        if (url.Length == 0)
            return new NzbGetResult.Error(NewznabError.IncorrectParameter);

        var subtitleUrl = parts.Length > 2 && parts[2].Length > 0 ? parts[2] : null;
        var channel = parts.Length > 3 ? parts[3] : "";
        var duration = parts.Length > 4 ? parts[4] : "0";
        var size = parts.Length > 5 ? parts[5] : "0";
        var category = parts.Length > 6 && parts[6].Length > 0 ? parts[6] : null;

        var metas = new List<NzbMeta>
        {
            new() { Type = "title", Value = title },
            new() { Type = FunkArrHeaders.Url, Value = url },
            new() { Type = FunkArrHeaders.Channel, Value = channel },
            new() { Type = FunkArrHeaders.Duration, Value = duration },
            new() { Type = FunkArrHeaders.Size, Value = size },
        };

        if (subtitleUrl is not null)
            metas.Add(new NzbMeta { Type = FunkArrHeaders.SubtitleUrl, Value = subtitleUrl });

        if (category is not null)
            metas.Add(new NzbMeta { Type = FunkArrHeaders.Category, Value = category });

        var nzb = new Nzb { Head = new NzbHead { Metas = metas } };
        var xml = NewznabXmlResult.Serialize(nzb);

        return new NzbGetResult.Success(
            Encoding.UTF8.GetBytes(xml),
            $"funkarr-{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.nzb");
    }

    internal NzbParseResult? ParseNzb(Stream stream)
    {
        using var xmlReader = XmlReader.Create(stream, _xmlSettings);
        if (_nzbSerializer.Deserialize(xmlReader) is not Nzb nzb)
            return null;

        return new NzbParseResult(nzb);
    }
}

internal sealed class NzbParseResult(Nzb nzb)
{
    internal string? Meta(string type) => nzb.Head.Metas.FirstOrDefault(m => m.Type == type)?.Value;
}
