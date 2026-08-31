using System.Xml.Serialization;

namespace FunkArr.ArrApi;

[XmlRoot("nzb")]
public sealed class Nzb
{
    [XmlElement("head")]
    public NzbHead Head { get; init; } = new();

    [XmlElement("file")]
    public NzbFile File { get; init; } = new();
}

public sealed class NzbHead
{
    [XmlElement("meta")]
    public List<NzbMeta> Metas { get; init; } = [];
}

public sealed class NzbMeta
{
    [XmlAttribute("type")]
    public string Type { get; init; } = "";

    [XmlText]
    public string Value { get; init; } = "";
}

public sealed class NzbFile
{
    [XmlAttribute("post_id")]
    public string PostId { get; init; } = "1";

    [XmlElement("groups")]
    public NzbGroups Groups { get; init; } = new();

    [XmlElement("segments")]
    public NzbSegments Segments { get; init; } = new();
}

public sealed class NzbGroups
{
    [XmlElement("group")]
    public List<string> Items { get; init; } = ["a.b.mediathek"];
}

public sealed class NzbSegments
{
    [XmlElement("segment")]
    public List<NzbSegment> Items { get; init; } = [new()];
}

public sealed class NzbSegment
{
    [XmlAttribute("number")]
    public int Number { get; init; } = 1;

    [XmlText]
    public string Value { get; init; } = "FunkArr@news.example.com";
}
