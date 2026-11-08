using System.Xml.Linq;
using FunkArr.ArrApi.Newznab;
using FunkArr.ArrApi.Newznab.Models;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class NewznabErrorTests
{
    [Fact]
    public void Error_xml_has_correct_structure()
    {
        var xml = NewznabXmlResult.Serialize(NewznabError.InvalidApiKey);
        var doc = XDocument.Parse(xml);
        var root = doc.Root!;

        Assert.Equal("error", root.Name.LocalName);
        Assert.Equal("100", root.Attribute("code")!.Value);
        Assert.Equal("Invalid API Key", root.Attribute("description")!.Value);
    }

    [Fact]
    public void Missing_parameter_error()
    {
        var xml = NewznabXmlResult.Serialize(NewznabError.MissingParameter);
        var doc = XDocument.Parse(xml);

        Assert.Equal("200", doc.Root!.Attribute("code")!.Value);
        Assert.Equal("Missing parameter", doc.Root!.Attribute("description")!.Value);
    }

    [Fact]
    public void Incorrect_parameter_error()
    {
        var xml = NewznabXmlResult.Serialize(NewznabError.IncorrectParameter);
        var doc = XDocument.Parse(xml);

        Assert.Equal("201", doc.Root!.Attribute("code")!.Value);
    }

    [Fact]
    public void No_such_function_error()
    {
        var xml = NewznabXmlResult.Serialize(NewznabError.NoSuchFunction);
        var doc = XDocument.Parse(xml);

        Assert.Equal("202", doc.Root!.Attribute("code")!.Value);
        Assert.Equal("No such function", doc.Root!.Attribute("description")!.Value);
    }

    [Theory]
    [InlineData(101, "Account suspended")]
    [InlineData(102, "Insufficient privileges")]
    [InlineData(103, "Registration denied")]
    [InlineData(104, "Registrations closed")]
    [InlineData(203, "Function not available")]
    public void Extended_error_codes_produce_valid_xml(int expectedCode, string expectedDescription)
    {
        var error = expectedCode switch
        {
            101 => NewznabError.AccountSuspended,
            102 => NewznabError.InsufficientPrivileges,
            103 => NewznabError.RegistrationDenied,
            104 => NewznabError.RegistrationsClosed,
            203 => NewznabError.FunctionNotAvailable,
            _ => throw new ArgumentOutOfRangeException(nameof(expectedCode))
        };

        var xml = NewznabXmlResult.Serialize(error);
        var doc = XDocument.Parse(xml);

        Assert.Multiple(
            () => Assert.Equal(expectedCode.ToString(), doc.Root!.Attribute("code")!.Value),
            () => Assert.Equal(expectedDescription, doc.Root!.Attribute("description")!.Value));
    }
}
