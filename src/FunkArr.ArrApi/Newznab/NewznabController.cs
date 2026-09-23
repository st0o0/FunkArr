using FunkArr.ArrApi.Newznab.Models;
using Microsoft.AspNetCore.Mvc;

namespace FunkArr.ArrApi.Newznab;

[ApiController]
[Route("/index/api")]
[ServiceFilter(typeof(NewznabApiKeyFilter))]
public sealed class NewznabController(
    NewznabSearchService search,
    NzbService nzb) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Handle([FromQuery] IndexerRequest req)
    {
        return (req.T ?? "") switch
        {
            "caps" => NewznabXmlResult.From(new Caps()),
            "tvsearch" or "movie" or "search" =>
                await search.Search(req, BaseUrl, ApiKey),
            "get" => nzb.GetNzb(req.Id),
            _ => NewznabXmlResult.Error(NewznabError.NoSuchFunction),
        };
    }

    private string BaseUrl => $"{Request.Scheme}://{Request.Host}";
    private string ApiKey => Request.Query["apikey"].FirstOrDefault() ?? "";
}
