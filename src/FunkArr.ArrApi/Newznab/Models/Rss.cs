using System.Xml.Serialization;

namespace FunkArr.ArrApi.Newznab.Models;

[XmlRoot("rss")]
public sealed class Rss
{
    [XmlAttribute("version")]
    public string Version { get; init; } = "2.0";

    [XmlElement("channel")]
    public Channel Channel { get; init; } = new();
}

public sealed class Channel
{
    [XmlElement("title")]
    public string Title { get; init; } = "FunkArr";

    [XmlElement("description")]
    public string Description { get; init; } = "FunkArr API results";

    [XmlElement("response", Namespace = NewznabNamespace.Uri)]
    public NewznabResponse Response { get; init; } = new();

    [XmlElement("item")]
    public List<Item> Items { get; init; } = [];
}

public sealed class NewznabResponse
{
    [XmlAttribute("offset")]
    public int Offset { get; init; }

    [XmlAttribute("total")]
    public int Total { get; init; }
}

public sealed class Item
{
    [XmlElement("title")]
    public string Title { get; init; } = "";

    [XmlElement("guid")]
    public ItemGuid Guid { get; init; } = new();

    [XmlElement("link")]
    public string Link { get; init; } = "";

    [XmlElement("comments")]
    public string Comments { get; init; } = "";

    [XmlElement("pubDate")]
    public string PubDate { get; init; } = "";

    [XmlElement("category")]
    public string Category { get; init; } = "";

    [XmlElement("description")]
    public string Description { get; init; } = "";

    [XmlElement("enclosure")]
    public Enclosure Enclosure { get; init; } = new();

    [XmlElement("attr", Namespace = NewznabNamespace.Uri)]
    public List<NewznabAttribute> Attributes { get; init; } = [];
}

public sealed class ItemGuid
{
    [XmlAttribute("isPermaLink")]
    public bool IsPermaLink { get; init; } = true;

    [XmlText]
    public string Value { get; init; } = "";
}

public sealed class Enclosure
{
    [XmlAttribute("url")]
    public string Url { get; init; } = "";

    [XmlAttribute("length")]
    public long Length { get; init; }

    [XmlAttribute("type")]
    public string Type { get; init; } = "application/x-nzb";
}

public sealed class NewznabAttribute
{
    [XmlAttribute("name")]
    public string Name { get; init; } = "";

    [XmlAttribute("value")]
    public string Value { get; init; } = "";
}

internal static class NewznabNamespace
{
    public const string Uri = "http://www.newznab.com/DTD/2010/feeds/attributes/";
}
