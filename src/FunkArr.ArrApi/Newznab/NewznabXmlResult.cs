using System.Net.Mime;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using FunkArr.ArrApi.Newznab.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FunkArr.ArrApi.Newznab;

internal sealed class NewznabXmlResult(object value) : IActionResult
{
    private static readonly XmlSerializerNamespaces _namespaces = new(
        [new XmlQualifiedName("newznab", NewznabNamespace.Uri)]);

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var xml = Serialize(value);
        context.HttpContext.Response.ContentType = $"{MediaTypeNames.Application.Xml}; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(xml, Encoding.UTF8);
    }

    internal static string Serialize(object obj)
    {
        var serializer = new XmlSerializer(obj.GetType());
        using var writer = new Utf8StringWriter();
        using var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = false,
            OmitXmlDeclaration = false,
        });
        serializer.Serialize(xmlWriter, obj, _namespaces);
        return writer.ToString();
    }

    internal static NewznabXmlResult From<T>(T obj) where T : class => new(obj);

    internal static IActionResult Error(NewznabError error) =>
        new ContentResult
        {
            Content = Serialize(error),
            ContentType = MediaTypeNames.Application.Xml,
            StatusCode = error.Code switch
            {
                100 => 403,
                _ => 400,
            },
        };

    internal static NewznabXmlResult Empty(int offset) =>
        From(new Rss
        {
            Channel = new Channel
            {
                Response = new NewznabResponse { Offset = offset, Total = 0 },
                Items = [],
            },
        });

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
