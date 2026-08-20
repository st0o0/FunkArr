using System.Globalization;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Configuration;
using FunkArr.Search;
using FunkArr.Shared.Models;

namespace FunkArr.Indexer;

public static class NewznabEndpoints
{
    private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(30);

    public static void MapNewznabEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api")
            .AddEndpointFilter<ApiKeyFilter>();

        group.MapGet("/", HandleNewznabRequest);

        app.MapGet("/api/fake_nzb", HandleFakeNzbDownload);
    }

    private static async Task<IResult> HandleNewznabRequest(HttpContext context,
        ActorRegistry actorRegistry)
    {
        var query = context.Request.Query;
        var t = query["t"].FirstOrDefault()?.ToLowerInvariant();
        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        var searchActor = actorRegistry.Get<SearchActor>();

        return t switch
        {
            "caps" => Results.Content(
                NewznabXmlBuilder.BuildCapsResponse(baseUrl),
                "application/xml"),

            "tvsearch" => await HandleTvSearch(query, baseUrl, searchActor),
            "movie" => await HandleMovieSearch(query, baseUrl, searchActor),
            "search" => await HandleTextSearch(query, baseUrl, searchActor),

            _ => Results.Content(
                NewznabXmlBuilder.BuildErrorResponse(202, "No such function"),
                "application/xml"),
        };
    }

    private static async Task<IResult> HandleTvSearch(IQueryCollection query, string baseUrl, IActorRef searchActor)
    {
        var tvdbId = int.TryParse(query["tvdbid"].FirstOrDefault(), CultureInfo.InvariantCulture, out var id) ? id : 0;
        var season = int.TryParse(query["season"].FirstOrDefault(), CultureInfo.InvariantCulture, out var s)
            ? s
            : (int?)null;
        var episode = int.TryParse(query["ep"].FirstOrDefault(), CultureInfo.InvariantCulture, out var e)
            ? e
            : (int?)null;
        var q = query["q"].FirstOrDefault();

        var request = new SearchActor.TvSearchRequest(tvdbId, q, season, episode, q);
        var response = await searchActor.Ask<SearchActor.SearchResponse>(request, AskTimeout);

        var newznabResults = response.Results
            .Select(r => ToNewznabResult(r, baseUrl, tvdbId > 0 ? tvdbId : null, season, episode))
            .ToList();

        return Results.Content(
            NewznabXmlBuilder.BuildSearchResponse(newznabResults),
            "application/xml");
    }

    private static async Task<IResult> HandleMovieSearch(IQueryCollection query, string baseUrl, IActorRef searchActor)
    {
        var imdbId = query["imdbid"].FirstOrDefault();
        var q = query["q"].FirstOrDefault();

        var request = new SearchActor.MovieSearchRequest(imdbId, q);
        var response = await searchActor.Ask<SearchActor.SearchResponse>(request, AskTimeout);

        var newznabResults = response.Results
            .Select(r => ToNewznabResult(r, baseUrl, null, null, null))
            .ToList();

        return Results.Content(
            NewznabXmlBuilder.BuildSearchResponse(newznabResults),
            "application/xml");
    }

    private static async Task<IResult> HandleTextSearch(IQueryCollection query, string baseUrl, IActorRef searchActor)
    {
        var q = query["q"].FirstOrDefault() ?? string.Empty;

        var request = new SearchActor.TextSearchRequest(q);
        var response = await searchActor.Ask<SearchActor.SearchResponse>(request, AskTimeout);

        var newznabResults = response.Results
            .Select(r => ToNewznabResult(r, baseUrl, null, null, null))
            .ToList();

        return Results.Content(
            NewznabXmlBuilder.BuildSearchResponse(newznabResults),
            "application/xml");
    }

    private static NewznabResult ToNewznabResult(
        SearchResult result, string baseUrl, int? tvdbId, int? season, int? episode)
    {
        var qi = result.QualityInfo;
        var codec = qi?.Codec ?? "h264";
        var qualityTier = qi?.QualityTier ?? result.Quality;

        // When estimated, use conservative 720p for HD tier
        if (qi is null or { ProbeSource: ProbeSource.Estimated })
        {
            qualityTier = result.Quality == QualityTier.HD1080 ? QualityTier.HD720 : result.Quality;
        }

        var releaseTitle = season is not null && episode is not null
            ? NewznabXmlBuilder.BuildReleaseTitle(result.Topic, season.Value, episode.Value, qualityTier, codec)
            : $"{result.Topic.Replace(' ', '.')}.GERMAN.{NewznabXmlBuilder.QualityString(qualityTier)}.WEB.{codec}-FA";

        var downloadUrl = BuildFakeNzbUrl(baseUrl, result.Url, releaseTitle, result.UrlSubtitle);

        var category = qualityTier == QualityTier.SD ? "5050" : "5040";

        var sizeBytes = qi?.FileSize ?? result.SizeBytes;

        return new NewznabResult
        {
            Title = releaseTitle,
            DownloadUrl = downloadUrl,
            SizeBytes = sizeBytes,
            PublishDate = result.Timestamp,
            Category = category,
            Guid = $"funkarr-{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(result.Url))[..16]}",
            QualityInfo = qi,
            TvdbId = tvdbId,
            Season = season,
            Episode = episode,
        };
    }

    private static string BuildFakeNzbUrl(string baseUrl, string videoUrl, string title, string? subtitleUrl)
    {
        var encodedUrl = Uri.EscapeDataString(
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(videoUrl)));
        var encodedTitle = Uri.EscapeDataString(
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(title)));

        var url = $"{baseUrl}/api/fake_nzb?url={encodedUrl}&title={encodedTitle}";

        if (subtitleUrl is not null)
        {
            var encodedSub = Uri.EscapeDataString(
                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(subtitleUrl)));
            url += $"&subtitle={encodedSub}";
        }

        return url;
    }

    private static IResult HandleFakeNzbDownload(HttpContext context)
    {
        var urlParam = context.Request.Query["url"].FirstOrDefault();
        var titleParam = context.Request.Query["title"].FirstOrDefault();

        if (string.IsNullOrEmpty(urlParam) || string.IsNullOrEmpty(titleParam))
        {
            return Results.BadRequest("Missing url or title parameter");
        }

        var downloadUrl = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(Uri.UnescapeDataString(urlParam)));
        var title = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(Uri.UnescapeDataString(titleParam)));

        var subtitleParam = context.Request.Query["subtitle"].FirstOrDefault();
        string? subtitleUrl = null;
        if (!string.IsNullOrEmpty(subtitleParam))
        {
            subtitleUrl = System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(Uri.UnescapeDataString(subtitleParam)));
        }

        var nzbContent = FakeNzbBuilder.BuildFakeNzbXml(downloadUrl, title, subtitleUrl);
        return Results.Content(nzbContent, "application/x-nzb");
    }
}
