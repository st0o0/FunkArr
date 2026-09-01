using System.Xml.Serialization;
using FunkArr.ArrApi.Sabnzbd.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FunkArr.ArrApi.Sabnzbd;

public static class DownloadApiEndpoints
{
    private static readonly XmlSerializer _nzbSerializer = new(typeof(Nzb));

    public static WebApplication MapDownloadApi(this WebApplication app)
    {
        var group = app.MapGroup("/download/api")
            .AddEndpointFilter(new ApiKeyEndpointFilter(
                () => Results.Json(new { status = false, error = "API Key Incorrect" }, statusCode: 403)));

        group.MapGet("/", ([AsParameters] DownloadGetRequest req, IConfiguration configuration) =>
        {
            var downloadPath = configuration["FunkArr:DownloadPath"] ?? "downloads";

            return (req.Mode ?? "") switch
            {
                "version" => Results.Json(new { version = "4.3.3" }),
                "get_config" => Results.Json(new
                {
                    config = new
                    {
                        misc = new
                        {
                            complete_dir = downloadPath.Replace('\\', '/'),
                            enable_tv_sorting = false,
                            enable_movie_sorting = false,
                            enable_date_sorting = false,
                            pre_check = false,
                            history_retention = "all",
                            tv_categories = Array.Empty<string>(),
                            movie_categories = Array.Empty<string>(),
                            date_categories = Array.Empty<string>(),
                        },
                        categories = Array.Empty<object>(),
                        sorters = Array.Empty<object>(),
                    },
                }),
                "fullstatus" => Results.Json(new FullStatusResponse(new FullStatusData(
                    Paused: false,
                    Speedlimit: "",
                    Diskspace1: "0",
                    Diskspace2: "0",
                    Completedir: downloadPath.Replace('\\', '/')))),
                "queue" when req.Name == "delete" =>
                    Results.Json(new { status = false, error = "Item not found" }),
                "queue" when req.Name is not null =>
                    Results.Json(new { status = false, error = "Invalid queue command" }, statusCode: 400),
                "queue" => Results.Json(new QueueResponse(new QueueData(
                    Paused: false,
                    Speedlimit: "",
                    NoofSlotsTotal: 0,
                    Diskspace1: "0",
                    Diskspace2: "0",
                    Speed: "0",
                    Slots: []))),
                "history" when req.Name == "delete" && !string.IsNullOrEmpty(req.Value) =>
                    Results.Json(new { status = false, error = "Item not found" }),
                "history" => Results.Json(new HistoryResponse(new HistoryData(
                    NoofSlots: 0,
                    Slots: []))),
                "retry" when string.IsNullOrEmpty(req.Value) =>
                    Results.Json(new { status = false, error = "Missing value parameter" }, statusCode: 400),
                "retry" => Results.Json(new { status = false, error = "Item not found" }),
                _ => Results.Json(new { status = false, error = "Invalid mode" }, statusCode: 400),
            };
        });

        group.MapPost("/", async ([AsParameters] DownloadPostRequest req, IFormFile? nzbfile) =>
        {
            if ((req.Mode ?? "") != "addfile")
            {
                return Results.Json(new { status = false, error = "Invalid mode" }, statusCode: 400);
            }

            if (nzbfile is null)
            {
                return Results.Json(new { status = false, error = "No NZB file uploaded" }, statusCode: 400);
            }

            using var stream = nzbfile.OpenReadStream();
            var nzb = _nzbSerializer.Deserialize(stream) as Nzb;

            var url = nzb?.Head?.Metas
                .FirstOrDefault(m => m.Type == "url")?.Value;

            if (url is null)
            {
                return Results.Json(new { status = false, error = "Invalid NZB format" }, statusCode: 400);
            }

            return Results.Json(new { status = true, nzo_ids = Array.Empty<string>() });
        }).DisableAntiforgery();

        return app;
    }
}
