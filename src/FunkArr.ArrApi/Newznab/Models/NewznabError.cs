using System.Xml.Serialization;

namespace FunkArr.ArrApi.Newznab.Models;

[XmlRoot("error")]
public sealed class NewznabError
{
    [XmlAttribute("code")]
    public int Code { get; init; }

    [XmlAttribute("description")]
    public string Description { get; init; } = "";

    public static NewznabError InvalidApiKey => new() { Code = 100, Description = "Invalid API Key" };
    public static NewznabError MissingParameter => new() { Code = 200, Description = "Missing parameter" };
    public static NewznabError IncorrectParameter => new() { Code = 201, Description = "Incorrect parameter" };
    public static NewznabError NoSuchFunction => new() { Code = 202, Description = "No such function" };
    public static NewznabError UnknownError(string reason) => new() { Code = 900, Description = reason };
}
