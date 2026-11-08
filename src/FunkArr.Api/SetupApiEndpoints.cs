using FunkArr.Api.Infrastructure;
using FunkArr.Api.Models.Setup;
using FunkArr.Api.Validation;
using FunkArr.Core.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FunkArr.Api;

public static class SetupApiEndpoints
{
    public static WebApplication MapSetupApi(this WebApplication app)
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
            var (selfUri, error) = ResolveSelfUrl(request, options.CurrentValue, ctx);
            if (error is not null) return Results.Ok(error);

            var payload = ProwlarrIndexerPayload.Create(selfUri!, options.CurrentValue.ApiKey);
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
            var (selfUri, error) = ResolveSelfUrl(request, options.CurrentValue, ctx);
            if (error is not null) return Results.Ok(error);

            var payload = SonarrRadarrIndexerPayload.Create(selfUri!, options.CurrentValue.ApiKey, [5030, 5040]);
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
            var (selfUri, error) = ResolveSelfUrl(request, options.CurrentValue, ctx);
            if (error is not null) return Results.Ok(error);

            var payload = SabnzbdDownloadClientPayload.Create(selfUri!, options.CurrentValue.ApiKey, "tv");
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
            var (selfUri, error) = ResolveSelfUrl(request, options.CurrentValue, ctx);
            if (error is not null) return Results.Ok(error);

            var payload = SonarrRadarrIndexerPayload.Create(selfUri!, options.CurrentValue.ApiKey, [2030, 2040]);
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
            var (selfUri, error) = ResolveSelfUrl(request, options.CurrentValue, ctx);
            if (error is not null) return Results.Ok(error);

            var payload = SabnzbdDownloadClientPayload.Create(selfUri!, options.CurrentValue.ApiKey, "movies");
            return Results.Ok(await client.PostResourceAsync(
                new ArrConnection(request.Url, request.ApiKey), "/api/v3/downloadclient", payload));
        })
        .WithSummary("Create FunkArr download client in Radarr")
        .WithDescription("Registers FunkArr as a SABnzbd download client in a Radarr instance.")
        .Produces<ArrResourceResponse>();

        return app;
    }

    internal static (Uri? SelfUri, ArrResourceResponse? Error) ResolveSelfUrl(
        CreateArrResourceRequest request, FunkArrOptions options, HttpContext ctx)
    {
        var raw = !string.IsNullOrWhiteSpace(request.FunkArrUrl)
            ? request.FunkArrUrl.TrimEnd('/')
            : !string.IsNullOrWhiteSpace(options.BaseUrl)
                ? options.BaseUrl.TrimEnd('/')
                : $"{ctx.Request.Scheme}://{ctx.Request.Host}";

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            return (null, new ArrResourceResponse(false,
                Error: "Invalid FunkArr URL -- must start with http:// or https://"));
        }

        return (uri, null);
    }
}
