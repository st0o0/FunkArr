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
    public static NewznabError AccountSuspended => new() { Code = 101, Description = "Account suspended" };
    public static NewznabError InsufficientPrivileges => new() { Code = 102, Description = "Insufficient privileges" };
    public static NewznabError RegistrationDenied => new() { Code = 103, Description = "Registration denied" };
    public static NewznabError RegistrationsClosed => new() { Code = 104, Description = "Registrations closed" };
    public static NewznabError MissingParameter => new() { Code = 200, Description = "Missing parameter" };
    public static NewznabError IncorrectParameter => new() { Code = 201, Description = "Incorrect parameter" };
    public static NewznabError NoSuchFunction => new() { Code = 202, Description = "No such function" };
    public static NewznabError FunctionNotAvailable => new() { Code = 203, Description = "Function not available" };
    public static NewznabError UnknownError(string reason) => new() { Code = 900, Description = reason };
}
