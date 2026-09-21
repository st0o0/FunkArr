using FunkArr.Api.Models;
using FunkArr.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FunkArr.Api;

public static class SetupArrEndpoints
{
    public static WebApplication MapSetupArrApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/setup")
            .WithTags("Setup")
            .AddEndpointFilter<EndpointExceptionFilter>();

        group.MapPost("/prowlarr/indexer", async (
            CreateArrResourceRequest request,
            IOptionsMonitor<FunkArrOptions> options,
            ArrApiClient client,
            HttpContext ctx) =>
        {
            var selfUrl = ResolveSelfUrl(request, ctx);
            var payload = ProwlarrIndexerPayload.Create(selfUrl, options.CurrentValue.ApiKey);
            return Results.Ok(await client.PostResourceAsync(
                new ArrConnection(request.Url, request.ApiKey), "/api/v1/indexer", payload));
        }).WithSummary("Create FunkArr indexer in Prowlarr");

        group.MapPost("/sonarr/indexer", async (
            CreateArrResourceRequest request,
            IOptionsMonitor<FunkArrOptions> options,
            ArrApiClient client,
            HttpContext ctx) =>
        {
            var selfUrl = ResolveSelfUrl(request, ctx);
            var payload = SonarrRadarrIndexerPayload.Create(selfUrl, options.CurrentValue.ApiKey, [5030, 5040]);
            return Results.Ok(await client.PostResourceAsync(
                new ArrConnection(request.Url, request.ApiKey), "/api/v3/indexer", payload));
        }).WithSummary("Create FunkArr indexer in Sonarr");

        group.MapPost("/sonarr/download-client", async (
            CreateArrResourceRequest request,
            IOptionsMonitor<FunkArrOptions> options,
            ArrApiClient client,
            HttpContext ctx) =>
        {
            var selfUrl = ResolveSelfUrl(request, ctx);
            var payload = SabnzbdDownloadClientPayload.Create(selfUrl, options.CurrentValue.ApiKey, "tv");
            return Results.Ok(await client.PostResourceAsync(
                new ArrConnection(request.Url, request.ApiKey), "/api/v3/downloadclient", payload));
        }).WithSummary("Create FunkArr download client in Sonarr");

        group.MapPost("/radarr/indexer", async (
            CreateArrResourceRequest request,
            IOptionsMonitor<FunkArrOptions> options,
            ArrApiClient client,
            HttpContext ctx) =>
        {
            var selfUrl = ResolveSelfUrl(request, ctx);
            var payload = SonarrRadarrIndexerPayload.Create(selfUrl, options.CurrentValue.ApiKey, [2030, 2040]);
            return Results.Ok(await client.PostResourceAsync(
                new ArrConnection(request.Url, request.ApiKey), "/api/v3/indexer", payload));
        }).WithSummary("Create FunkArr indexer in Radarr");

        group.MapPost("/radarr/download-client", async (
            CreateArrResourceRequest request,
            IOptionsMonitor<FunkArrOptions> options,
            ArrApiClient client,
            HttpContext ctx) =>
        {
            var selfUrl = ResolveSelfUrl(request, ctx);
            var payload = SabnzbdDownloadClientPayload.Create(selfUrl, options.CurrentValue.ApiKey, "movies");
            return Results.Ok(await client.PostResourceAsync(
                new ArrConnection(request.Url, request.ApiKey), "/api/v3/downloadclient", payload));
        }).WithSummary("Create FunkArr download client in Radarr");

        return app;
    }

    private static string ResolveSelfUrl(CreateArrResourceRequest request, HttpContext ctx)
    {
        return string.IsNullOrWhiteSpace(request.FunkArrUrl)
            ? $"{ctx.Request.Scheme}://{ctx.Request.Host}"
            : request.FunkArrUrl.TrimEnd('/');
    }
}
