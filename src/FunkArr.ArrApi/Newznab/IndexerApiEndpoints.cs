using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Akka.Actor;
using FunkArr.ArrApi.Newznab.Models;
using FunkArr.Messages.Search;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FunkArr.ArrApi.Newznab;

public static class IndexerApiEndpoints
{
    private static readonly XmlSerializerNamespaces _namespaces = new(
        [new XmlQualifiedName("newznab", NewznabNamespace.Uri)]);

    private static readonly TimeSpan _searchTimeout = TimeSpan.FromSeconds(30);

    public static WebApplication MapIndexerApi(this WebApplication app, string apiKey, IActorRef searchGateway)
    {
        var group = app.MapGroup("/index/api")
            .AddEndpointFilter(new ApiKeyEndpointFilter(apiKey,
                () => ErrorResult(NewznabError.InvalidApiKey)));

        group.MapGet("/", async ([AsParameters] IndexerRequest req) =>
        {
            return (req.T ?? "") switch
            {
                "caps" => XmlResult(Serialize(new Caps())),
                "tvsearch" => await HandleTvSearch(searchGateway, req),
                "movie" => await HandleMovieSearch(searchGateway, req),
                "search" => await HandleGeneralSearch(searchGateway, req),
                "get" => NzbGetResult(req.Id),
                _ => ErrorResult(NewznabError.NoSuchFunction),
            };
        });

        return app;
    }

    private static async Task<IResult> HandleTvSearch(IActorRef gateway, IndexerRequest req)
    {
        var cmd = new TvSearchCommand(
            Guid.Empty, req.Q,
            ParseInt(req.Season), ParseInt(req.Ep),
            ParseInt(req.TvdbId), req.ImdbId);

        return await AskAndFormat(gateway, cmd, req.Offset ?? 0);
    }

    private static async Task<IResult> HandleMovieSearch(IActorRef gateway, IndexerRequest req)
    {
        var cmd = new MovieSearchCommand(
            Guid.Empty, req.Q, req.ImdbId, ParseInt(req.TmdbId));

        return await AskAndFormat(gateway, cmd, req.Offset ?? 0);
    }

    private static async Task<IResult> HandleGeneralSearch(IActorRef gateway, IndexerRequest req)
    {
        var cmd = new GeneralSearchCommand(req.Q, ParseInt(req.Cat));
        return await AskAndFormat(gateway, cmd, req.Offset ?? 0);
    }

    private static async Task<IResult> AskAndFormat(IActorRef gateway, object cmd, int offset)
    {
        try
        {
            var response = await gateway.Ask<object>(cmd, _searchTimeout);
            return response switch
            {
                SearchCompleted completed => XmlResult(Serialize(ToRss(completed, offset))),
                SearchFailed failed => ErrorResult(NewznabError.UnknownError(failed.Reason)),
                _ => ErrorResult(NewznabError.UnknownError("Unexpected response")),
            };
        }
        catch (Exception)
        {
            return ErrorResult(NewznabError.UnknownError("Search timed out"));
        }
    }

    internal static Rss ToRss(SearchCompleted completed, int offset)
    {
        var items = completed.Items.Select(item =>
        {
            var nzbId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{item.Title}|{item.Url}"));
            return new Item
            {
                Title = item.Title,
                Guid = new ItemGuid { Value = nzbId, IsPermaLink = false },
                Link = item.Url,
                PubDate = item.AiredAt?.ToString("R") ?? "",
                Category = item.Quality >= 720 ? "TV > HD" : "TV > SD",
                Description = $"{item.Channel} - {item.Topic}",
                Enclosure = new Enclosure { Url = item.Url, Length = item.Size },
                Attributes =
                [
                    new NewznabAttribute { Name = "size", Value = item.Size.ToString() },
                    new NewznabAttribute { Name = "category", Value = item.Quality >= 720 ? "5040" : "5030" },
                ],
            };
        }).ToList();

        return new Rss
        {
            Channel = new Channel
            {
                Response = new NewznabResponse { Offset = offset, Total = completed.Total },
                Items = items,
            },
        };
    }

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

        var pipeIndex = decoded.IndexOf('|');
        if (pipeIndex < 0)
        {
            return ErrorResult(NewznabError.IncorrectParameter);
        }

        var title = decoded[..pipeIndex];
        var url = decoded[(pipeIndex + 1)..];

        var nzb = new Nzb
        {
            Head = new NzbHead
            {
                Metas =
                [
                    new NzbMeta { Type = "title", Value = title },
                    new NzbMeta { Type = "url", Value = url },
                ],
            },
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
        using var writer = new StringWriter();
        using var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = false,
            OmitXmlDeclaration = false,
        });
        serializer.Serialize(xmlWriter, obj, _namespaces);
        return writer.ToString();
    }

    private static IResult XmlResult(string xml) =>
        Results.Content(xml, "application/xml", Encoding.UTF8);
}
