using FunkArr.ArrApi.Sabnzbd.Models;
using FunkArr.ArrApi.Sabnzbd.Requests;
using FunkArr.ArrApi.Sabnzbd.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace FunkArr.ArrApi.Sabnzbd;

[ApiController]
[Route("/download/api")]
[RequestTimeout(milliseconds: 15_000)]
[ServiceFilter(typeof(SabnzbdApiKeyFilter))]
public sealed class SabnzbdController(
    SabnzbdQueueService queue,
    SabnzbdDownloadService downloads) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> HandleGet([FromQuery] DownloadGetRequest req, CancellationToken cancellationToken)
    {
        return (req.Mode ?? "") switch
        {
            "version" => Ok(new SabnzbdVersionResponse(SabnzbdConstants.Version)),
            "get_config" => MapResult(queue.GetConfig()),
            "fullstatus" => MapResult(await queue.GetFullStatus(cancellationToken)),
            "queue" when req.Name == "delete" && !string.IsNullOrEmpty(req.Value) =>
                MapResult(await downloads.DeleteFromQueue(req.Value, cancellationToken)),
            "queue" when req.Name is "priority" && !string.IsNullOrEmpty(req.Value) =>
                MapResult(await downloads.SetPriority(req.Value, req.Value2, cancellationToken)),
            "queue" when req.Name is "switch" && !string.IsNullOrEmpty(req.Value) && !string.IsNullOrEmpty(req.Value2) =>
                MapResult(await downloads.Swap(req.Value, req.Value2, cancellationToken)),
            "queue" when req.Name is not null =>
                Ok(new SabnzbdErrorResponse(false, "Invalid queue command")),
            "queue" => MapResult(await queue.GetQueue(req.Start ?? 0, req.Limit ?? 0, req.Category, cancellationToken)),
            "history" when req.Name is "delete" && !string.IsNullOrEmpty(req.Value) =>
                MapResult(await downloads.DeleteFromHistory(req.Value, cancellationToken)),
            "history" => MapResult(await queue.GetHistory(req.Start ?? 0, req.Limit ?? 0, req.Category, cancellationToken)),
            "retry" when string.IsNullOrEmpty(req.Value) =>
                Ok(new SabnzbdErrorResponse(false, "Missing value parameter")),
            "retry" => MapResult(await downloads.Retry(req.Value, cancellationToken)),
            "pause" => MapResult(await queue.PauseQueue(cancellationToken)),
            "resume" => MapResult(await queue.ResumeQueue(cancellationToken)),
            _ => Ok(new SabnzbdErrorResponse(false, "Invalid mode"))
        };
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> HandlePost([FromQuery] DownloadPostRequest req, IFormFile? name, CancellationToken cancellationToken)
    {
        if ((req.Mode ?? "") != "addfile")
        {
            return Ok(new SabnzbdErrorResponse(false, "Invalid mode"));
        }

        return MapResult(await downloads.AddFile(name, req.Cat, req.Priority, cancellationToken));
    }

    private ObjectResult MapResult<T>(SabnzbdResult<T> result) => result switch
    {
        SabnzbdResult<T>.Ok ok => Ok(ok.Data),
        SabnzbdResult<T>.Error err => Ok(new SabnzbdErrorResponse(false, err.Message)),
        _ => Ok(new SabnzbdErrorResponse(false, "Unexpected result"))
    };
}
