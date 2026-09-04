using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Akka.Hosting;
using FunkArr.ArrApi.Newznab.Models;
using FunkArr.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FunkArr.ArrApi.Newznab;

public static class IndexerApiEndpoints
{
    private static readonly XmlSerializerNamespaces _namespaces = new(
        [new XmlQualifiedName("newznab", NewznabNamespace.Uri)]);

    internal const int MaxLimit = 500;
    internal const int DefaultLimit = 100;

    public static WebApplication MapIndexerApi(this WebApplication app)
    {
        var group = app.MapGroup("/index/api")
            .AddEndpointFilter(new ApiKeyEndpointFilter(
                () => ErrorResult(NewznabError.InvalidApiKey)));

        group.MapGet("/", async ([AsParameters] IndexerRequest req, IActorRegistry registry, HttpContext ctx) =>
        {
            return (req.T ?? "") switch
            {
                "caps" => XmlResult(Serialize(new Caps())),
                "tvsearch" or "movie" or "search" =>
                    await new SearchHandler(
                        await registry.GetAsync<ISearchManager>(),
                        $"{ctx.Request.Scheme}://{ctx.Request.Host}",
                        ctx.Request.Query["apikey"].FirstOrDefault() ?? "").Handle(req),
                "get" => NzbGetResult(req.Id),
                _ => ErrorResult(NewznabError.NoSuchFunction),
            };
        });

        return app;
    }

    internal static int? CapLimit(int? limit) => limit switch
    {
        null => null,
        > MaxLimit => MaxLimit,
        _ => limit,
    };

    internal static IResult EmptyResult(int offset) =>
        XmlResult(Serialize(new Rss
        {
            Channel = new Channel
            {
                Response = new NewznabResponse { Offset = offset, Total = 0 },
                Items = [],
            },
        }));

    internal static int? ParseInt(string? value) =>
        int.TryParse(value, out var result) ? result : null;

    private static IResult NzbGetResult(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return ErrorResult(NewznabError.MissingParameter);
        }

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(id));
        }
        catch (FormatException)
        {
            return ErrorResult(NewznabError.IncorrectParameter);
        }

        var parts = decoded.Split('\t');
        if (parts.Length < 2)
        {
            return ErrorResult(NewznabError.IncorrectParameter);
        }

        var title = parts[0];
        var url = parts[1];
        var subtitleUrl = parts.Length > 2 && parts[2].Length > 0 ? parts[2] : null;
        var channel = parts.Length > 3 ? parts[3] : "";
        var duration = parts.Length > 4 ? parts[4] : "0";
        var size = parts.Length > 5 ? parts[5] : "0";
        var category = parts.Length > 6 && parts[6].Length > 0 ? parts[6] : null;

        var metas = new List<NzbMeta>
        {
            new() { Type = "title", Value = title },
            new() { Type = "X-FunkArr-Url", Value = url },
            new() { Type = "X-FunkArr-Channel", Value = channel },
            new() { Type = "X-FunkArr-Duration", Value = duration },
            new() { Type = "X-FunkArr-Size", Value = size },
        };

        if (subtitleUrl is not null)
        {
            metas.Add(new NzbMeta { Type = "X-FunkArr-SubtitleUrl", Value = subtitleUrl });
        }

        if (category is not null)
        {
            metas.Add(new NzbMeta { Type = "X-FunkArr-Category", Value = category });
        }

        var nzb = new Nzb
        {
            Head = new NzbHead { Metas = metas },
        };

        return Results.File(
            Encoding.UTF8.GetBytes(Serialize(nzb)),
            "application/x-nzb",
            $"funkarr-{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.nzb");
    }

    internal static IResult ErrorResult(NewznabError error) =>
        Results.Content(Serialize(error), "application/xml", Encoding.UTF8, error.Code switch
        {
            100 => 403,
            _ => 400,
        });

    internal static string Serialize<T>(T obj) where T : class
    {
        var serializer = new XmlSerializer(typeof(T));
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

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

    internal static IResult XmlResult(string xml) =>
        Results.Content(xml, "application/xml", Encoding.UTF8);
}
