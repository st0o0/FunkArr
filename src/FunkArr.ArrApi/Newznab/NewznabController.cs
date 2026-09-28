using FunkArr.ArrApi.Newznab.Models;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace FunkArr.ArrApi.Newznab;

[ApiController]
[Route("/index/api")]
[HttpLogging(HttpLoggingFields.All)]
[RequestTimeout(milliseconds: 45_000)]
[ServiceFilter(typeof(NewznabApiKeyFilter))]
public sealed class NewznabController(NewznabSearchService search, TimeProvider timeProvider) : ControllerBase
{
    private const string _applicationNzb = "application/x-nzb";

    [HttpGet]
    public async Task<IActionResult> Handle([FromQuery] IndexerRequest req, CancellationToken cancellationToken)
    {
        return (req.T ?? "") switch
        {
            "caps" => NewznabXmlResult.From(new Caps()),
            "tvsearch" or "movie" or "search" => MapSearchResult(await search.Search(req, BaseUrl, ApiKey, cancellationToken)),
            "get" => MapNzbResult(NzbService.GetNzb(req.Id, timeProvider)),
            _ => NewznabXmlResult.Error(NewznabError.NoSuchFunction)
        };
    }

    private static IActionResult MapSearchResult(SearchServiceResult result) => result switch
    {
        SearchServiceResult.Success ok => NewznabXmlResult.From(ok.Rss),
        SearchServiceResult.Empty empty => NewznabXmlResult.Empty(empty.Offset),
        SearchServiceResult.Failed failed => NewznabXmlResult.Error(NewznabError.UnknownError(failed.Message)),
        _ => NewznabXmlResult.Error(NewznabError.UnknownError("Unexpected result"))
    };

    private static IActionResult MapNzbResult(NzbGetResult result) => result switch
    {
        NzbGetResult.Success ok => new FileContentResult(ok.Content, _applicationNzb) { FileDownloadName = ok.FileName },
        NzbGetResult.Error err => NewznabXmlResult.Error(err.ErrorDetail),
        _ => NewznabXmlResult.Error(NewznabError.UnknownError("Unexpected result"))
    };

    private string BaseUrl => $"{Request.Scheme}://{Request.Host}";
    private string ApiKey => HttpContext.Items["ApiKey"] as string ?? "";
}
