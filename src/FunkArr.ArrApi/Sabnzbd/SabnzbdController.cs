using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FunkArr.ArrApi.Sabnzbd;

[ApiController]
[Route("/download/api")]
[ServiceFilter(typeof(SabnzbdApiKeyFilter))]
public sealed class SabnzbdController(
    SabnzbdQueueService queue,
    SabnzbdDownloadService downloads) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> HandleGet([FromQuery] DownloadGetRequest req)
    {
        return (req.Mode ?? "") switch
        {
            "version" => Ok(new { version = SabnzbdConstants.Version }),
            "get_config" => MapResult(queue.GetConfig()),
            "fullstatus" => MapResult(await queue.GetFullStatus()),
            "queue" when req.Name == "delete" && !string.IsNullOrEmpty(req.Value) =>
                MapResult(await downloads.DeleteFromQueue(req.Value)),
            "queue" when req.Name == "priority" && !string.IsNullOrEmpty(req.Value) =>
                MapResult(await downloads.SetPriority(req.Value, req.Value2)),
            "queue" when req.Name == "switch" && !string.IsNullOrEmpty(req.Value) && !string.IsNullOrEmpty(req.Value2) =>
                MapResult(await downloads.Swap(req.Value, req.Value2)),
            "queue" when req.Name is not null =>
                BadRequest(new { status = false, error = "Invalid queue command" }),
            "queue" => MapResult(await queue.GetQueue(req.Start ?? 0, req.Limit ?? 0, req.Category)),
            "history" when req.Name == "delete" && !string.IsNullOrEmpty(req.Value) =>
                MapResult(await downloads.DeleteFromHistory(req.Value)),
            "history" => MapResult(await queue.GetHistory(req.Start ?? 0, req.Limit ?? 0, req.Category)),
            "retry" when string.IsNullOrEmpty(req.Value) =>
                BadRequest(new { status = false, error = "Missing value parameter" }),
            "retry" => MapResult(await downloads.Retry(req.Value)),
            "pause" => MapResult(await queue.PauseQueue()),
            "resume" => MapResult(await queue.ResumeQueue()),
            _ => BadRequest(new { status = false, error = "Invalid mode" }),
        };
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> HandlePost(
        [FromQuery] DownloadPostRequest req,
        IFormFile? name)
    {
        if ((req.Mode ?? "") != "addfile")
            return BadRequest(new { status = false, error = "Invalid mode" });

        return MapResult(await downloads.AddFile(name, req.Cat, req.Priority));
    }

    private IActionResult MapResult(SabnzbdResult result) => result switch
    {
        SabnzbdResult.Ok ok => Ok(ok.Data),
        SabnzbdResult.Error err => new ObjectResult(new { status = false, error = err.Message }) { StatusCode = err.StatusCode },
        _ => StatusCode(500, new { status = false, error = "Unexpected result" }),
    };
}
