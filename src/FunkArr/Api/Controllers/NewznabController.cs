using Akka.Actor;
using Akka.Hosting;
using FunkArr.Indexer;
using FunkArr.Search;
using FunkArr.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FunkArr.Api.Controllers;

[ApiController]
[Route("index/api")]
[Route("api")]
[Route("api/api")]
[Tags("Newznab Emulation")]
public sealed class NewznabController(ActorRegistry actorRegistry) : ControllerBase
{
    private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(30);

    [HttpGet("")]
    [Produces("application/xml")]
    public async Task<IActionResult> HandleNewznabRequest(
        [FromQuery] string? t,
        [FromQuery] string? q,
        [FromQuery] int? tvdbid,
        [FromQuery] int? season,
        [FromQuery(Name = "ep")] int? episode,
        [FromQuery] string? imdbid)
    {
        var mode = t?.ToLowerInvariant();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var searchActor = await actorRegistry.GetAsync<SearchCoordinator>();

        return mode switch
        {
            "caps" => Content(
                NewznabXmlBuilder.BuildCapsResponse(baseUrl),
                "application/xml"),

            "tvsearch" => await HandleTvSearch(tvdbid ?? 0, q, season, episode, baseUrl, searchActor),
            "movie" => await HandleMovieSearch(imdbid, q, baseUrl, searchActor),
            "search" => await HandleTextSearch(q, baseUrl, searchActor),

            _ => Content(
                NewznabXmlBuilder.BuildErrorResponse(202, "No such function"),
                "application/xml"),
        };
    }

    [HttpGet("fake_nzb")]
    public IActionResult HandleFakeNzbDownload(
        [FromQuery] string? url,
        [FromQuery] string? title,
        [FromQuery] string? subtitle)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(title))
        {
            return BadRequest("Missing url or title parameter");
        }

        var downloadUrl = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(Uri.UnescapeDataString(url)));
        var decodedTitle = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(Uri.UnescapeDataString(title)));

        string? subtitleUrl = null;
        if (!string.IsNullOrEmpty(subtitle))
        {
            subtitleUrl = System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(Uri.UnescapeDataString(subtitle)));
        }

        var nzbContent = FakeNzbBuilder.BuildFakeNzbXml(downloadUrl, decodedTitle, subtitleUrl);
        return Content(nzbContent, "application/x-nzb");
    }

    private async Task<IActionResult> HandleTvSearch(
        int tvdbId, string? q, int? season, int? episode, string baseUrl, IActorRef searchActor)
    {
        if (tvdbId == 0 && string.IsNullOrWhiteSpace(q))
        {
            return await HandleRssFeed(baseUrl, searchActor);
        }

        var request = new SearchCoordinator.TvSearchRequest(tvdbId, q, season, episode, q);
        var response = await searchActor.Ask<SearchCoordinator.SearchResponse>(request, AskTimeout);

        var newznabResults = response.Results
            .Select(r => ToNewznabResult(r, baseUrl, tvdbId > 0 ? tvdbId : null, season, episode))
            .ToList();

        return Content(
            NewznabXmlBuilder.BuildSearchResponse(newznabResults),
            "application/xml");
    }

    private async Task<IActionResult> HandleMovieSearch(
        string? imdbId, string? q, string baseUrl, IActorRef searchActor)
    {
        var request = new SearchCoordinator.MovieSearchRequest(imdbId, q);
        var response = await searchActor.Ask<SearchCoordinator.SearchResponse>(request, AskTimeout);

        var newznabResults = response.Results
            .Select(r => ToNewznabResult(r, baseUrl, null, null, null))
            .ToList();

        return Content(
            NewznabXmlBuilder.BuildSearchResponse(newznabResults),
            "application/xml");
    }

    private async Task<IActionResult> HandleTextSearch(
        string? q, string baseUrl, IActorRef searchActor)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return await HandleRssFeed(baseUrl, searchActor);
        }

        var request = new SearchCoordinator.TextSearchRequest(q);
        var response = await searchActor.Ask<SearchCoordinator.SearchResponse>(request, AskTimeout);

        var newznabResults = response.Results
            .Select(r => ToNewznabResult(r, baseUrl, null, null, null))
            .ToList();

        return Content(
            NewznabXmlBuilder.BuildSearchResponse(newznabResults),
            "application/xml");
    }

    private async Task<IActionResult> HandleRssFeed(string baseUrl, IActorRef searchActor)
    {
        var request = new SearchCoordinator.TextSearchRequest(string.Empty);
        var response = await searchActor.Ask<SearchCoordinator.SearchResponse>(request, AskTimeout);

        var newznabResults = response.Results
            .Select(r => ToNewznabResult(r, baseUrl, null, null, null))
            .ToList();

        return Content(
            NewznabXmlBuilder.BuildSearchResponse(newznabResults),
            "application/xml");
    }

    private static NewznabResult ToNewznabResult(
        SearchResult result, string baseUrl, int? tvdbId, int? season, int? episode)
    {
        var qi = result.QualityInfo;
        var codec = qi?.Codec ?? "h264";
        var qualityTier = qi?.QualityTier ?? result.Quality;

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

        var url = $"{baseUrl}/index/api/fake_nzb?url={encodedUrl}&title={encodedTitle}";

        if (subtitleUrl is not null)
        {
            var encodedSub = Uri.EscapeDataString(
                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(subtitleUrl)));
            url += $"&subtitle={encodedSub}";
        }

        return url;
    }
}
