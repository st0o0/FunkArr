using System.Xml.Serialization;

namespace FunkArr.ArrApi.Newznab.Models;

[XmlRoot("caps")]
public sealed class Caps
{
    [XmlElement("server")]
    public Server Server { get; init; } = new();

    [XmlElement("limits")]
    public Limits Limits { get; init; } = new();

    [XmlElement("registration")]
    public Registration Registration { get; init; } = new();

    [XmlElement("searching")]
    public Searching Searching { get; init; } = new();

    [XmlElement("categories")]
    public Categories Categories { get; init; } = new();
}

public sealed class Server
{
    [XmlAttribute("title")]
    public string Title { get; init; } = "FunkArr";
}

public sealed class Limits
{
    [XmlAttribute("max")]
    public int Max { get; init; } = 500;

    [XmlAttribute("default")]
    public int Default { get; init; } = 100;
}

public sealed class Registration
{
    [XmlAttribute("available")]
    public string Available { get; init; } = "no";

    [XmlAttribute("open")]
    public string Open { get; init; } = "no";
}

public sealed class Searching
{
    [XmlElement("search")]
    public SearchType Search { get; init; } = new() { Available = "yes", SupportedParams = "q" };

    [XmlElement("tv-search")]
    public SearchType TvSearch { get; init; } = new() { Available = "yes", SupportedParams = "q,season,ep,tvdbid" };

    [XmlElement("movie-search")]
    public SearchType MovieSearch { get; init; } = new() { Available = "yes", SupportedParams = "q,imdbid,tmdbid" };

    [XmlElement("audio-search")]
    public SearchType AudioSearch { get; init; } = new() { Available = "no", SupportedParams = "" };

    [XmlElement("book-search")]
    public SearchType BookSearch { get; init; } = new() { Available = "no", SupportedParams = "" };
}

public sealed class SearchType
{
    [XmlAttribute("available")]
    public string Available { get; init; } = "no";

    [XmlAttribute("supportedParams")]
    public string SupportedParams { get; init; } = "";
}

public sealed class Categories
{
    [XmlElement("category")]
    public List<CategoryEntry> Items { get; init; } = [];
}

public sealed class CategoryEntry
{
    [XmlAttribute("id")]
    public int Id { get; init; }

    [XmlAttribute("name")]
    public string Name { get; init; } = "";

    [XmlElement("subcat")]
    public List<SubCategory> SubCategories { get; init; } = [];
}

public sealed class SubCategory
{
    [XmlAttribute("id")]
    public int Id { get; init; }

    [XmlAttribute("name")]
    public string Name { get; init; } = "";
}
