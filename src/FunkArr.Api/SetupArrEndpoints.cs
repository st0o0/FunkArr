using FunkArr.Api.Models;
using FunkArr.Api.Validation;
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
            .AddEndpointFilter<ValidationEndpointFilter>()
            .AddEndpointFilter<EndpointExceptionFilter>();

        group.MapPost("/prowlarr/indexer", async (
            CreateArrResourceRequest request,
            IOptionsMonitor<FunkArrOptions> options,
            ArrSetupClient client,
            HttpContext ctx) =>
        {
            var selfUrl = ResolveSelfUrl(request, ctx);
            var payload = ProwlarrIndexerPayload.Create(selfUrl, options.CurrentValue.ApiKey);
            return Results.Ok(await client.PostResourceAsync(
                new ArrConnection(request.Url, request.ApiKey), "/api/v1/indexer", payload));
        })
        .WithSummary("Create FunkArr indexer in Prowlarr")
        .WithDescription("Registers FunkArr as a Newznab indexer in a Prowlarr instance.")
        .Produces<ArrResourceResponse>();

        group.MapPost("/sonarr/indexer", async (
            CreateArrResourceRequest request,
            IOptionsMonitor<FunkArrOptions> options,
            ArrSetupClient client,
            HttpContext ctx) =>
        {
            var selfUrl = ResolveSelfUrl(request, ctx);
            var payload = SonarrRadarrIndexerPayload.Create(selfUrl, options.CurrentValue.ApiKey, [5030, 5040]);
            return Results.Ok(await client.PostResourceAsync(
                new ArrConnection(request.Url, request.ApiKey), "/api/v3/indexer", payload));
        })
        .WithSummary("Create FunkArr indexer in Sonarr")
        .WithDescription("Registers FunkArr as a Newznab indexer in a Sonarr instance.")
        .Produces<ArrResourceResponse>();

        group.MapPost("/sonarr/download-client", async (
            CreateArrResourceRequest request,
            IOptionsMonitor<FunkArrOptions> options,
            ArrSetupClient client,
            HttpContext ctx) =>
        {
            var selfUrl = ResolveSelfUrl(request, ctx);
            var payload = SabnzbdDownloadClientPayload.Create(selfUrl, options.CurrentValue.ApiKey, "tv");
            return Results.Ok(await client.PostResourceAsync(
                new ArrConnection(request.Url, request.ApiKey), "/api/v3/downloadclient", payload));
        })
        .WithSummary("Create FunkArr download client in Sonarr")
        .WithDescription("Registers FunkArr as a SABnzbd download client in a Sonarr instance.")
        .Produces<ArrResourceResponse>();

        group.MapPost("/radarr/indexer", async (
            CreateArrResourceRequest request,
            IOptionsMonitor<FunkArrOptions> options,
            ArrSetupClient client,
            HttpContext ctx) =>
        {
            var selfUrl = ResolveSelfUrl(request, ctx);
            var payload = SonarrRadarrIndexerPayload.Create(selfUrl, options.CurrentValue.ApiKey, [2030, 2040]);
            return Results.Ok(await client.PostResourceAsync(
                new ArrConnection(request.Url, request.ApiKey), "/api/v3/indexer", payload));
        })
        .WithSummary("Create FunkArr indexer in Radarr")
        .WithDescription("Registers FunkArr as a Newznab indexer in a Radarr instance.")
        .Produces<ArrResourceResponse>();

        group.MapPost("/radarr/download-client", async (
            CreateArrResourceRequest request,
            IOptionsMonitor<FunkArrOptions> options,
            ArrSetupClient client,
            HttpContext ctx) =>
        {
            var selfUrl = ResolveSelfUrl(request, ctx);
            var payload = SabnzbdDownloadClientPayload.Create(selfUrl, options.CurrentValue.ApiKey, "movies");
            return Results.Ok(await client.PostResourceAsync(
                new ArrConnection(request.Url, request.ApiKey), "/api/v3/downloadclient", payload));
        })
        .WithSummary("Create FunkArr download client in Radarr")
        .WithDescription("Registers FunkArr as a SABnzbd download client in a Radarr instance.")
        .Produces<ArrResourceResponse>();

        return app;
    }

    private static string ResolveSelfUrl(CreateArrResourceRequest request, HttpContext ctx)
    {
        return string.IsNullOrWhiteSpace(request.FunkArrUrl)
            ? $"{ctx.Request.Scheme}://{ctx.Request.Host}"
            : request.FunkArrUrl.TrimEnd('/');
    }
}
