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
            "version" => new JsonResult(new { version = SabnzbdConstants.Version }),
            "get_config" => queue.GetConfig(),
            "fullstatus" => await queue.GetFullStatus(),
            "queue" when req.Name == "delete" && !string.IsNullOrEmpty(req.Value) =>
                await downloads.DeleteFromQueue(req.Value),
            "queue" when req.Name == "priority" && !string.IsNullOrEmpty(req.Value) =>
                await downloads.SetPriority(req.Value, req.Value2),
            "queue" when req.Name == "switch" && !string.IsNullOrEmpty(req.Value) && !string.IsNullOrEmpty(req.Value2) =>
                await downloads.Swap(req.Value, req.Value2),
            "queue" when req.Name is not null =>
                new JsonResult(new { status = false, error = "Invalid queue command" }) { StatusCode = 400 },
            "queue" => await queue.GetQueue(req.Start ?? 0, req.Limit ?? 0, req.Category),
            "history" when req.Name == "delete" && !string.IsNullOrEmpty(req.Value) =>
                await downloads.DeleteFromHistory(req.Value),
            "history" => await queue.GetHistory(req.Start ?? 0, req.Limit ?? 0, req.Category),
            "retry" when string.IsNullOrEmpty(req.Value) =>
                new JsonResult(new { status = false, error = "Missing value parameter" }) { StatusCode = 400 },
            "retry" => await downloads.Retry(req.Value),
            _ => new JsonResult(new { status = false, error = "Invalid mode" }) { StatusCode = 400 },
        };
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> HandlePost(
        [FromQuery] DownloadPostRequest req,
        IFormFile? name)
    {
        if ((req.Mode ?? "") != "addfile")
            return new JsonResult(new { status = false, error = "Invalid mode" }) { StatusCode = 400 };

        return await downloads.AddFile(name, req.Cat, req.Priority);
    }
}
