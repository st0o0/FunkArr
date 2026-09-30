---
description: "Add API endpoints following FunkArr.Api's Minimal API pattern with actor Ask, response matching, and OpenAPI metadata (not for ArrApi controller-based endpoints)"
---

# FunkArr API Endpoint Pattern

## Path layout

- `/api/<resource>` — Internal REST API for UI (`FunkArr.Api`)
- `/index/api` — Newznab indexer API (`FunkArr.ArrApi`)
- `/download/api` — SABnzbd download client API (`FunkArr.ArrApi`)

New UI-facing endpoints go in `FunkArr.Api`.

## File structure

Each domain gets one `<Domain>ApiEndpoints.cs` file as a `static partial class` (or `static class` if no `[GeneratedRegex]`).

```
src/FunkArr.Api/
  <Domain>ApiEndpoints.cs      # Endpoint definitions
  Extensions/<Domain>MappingExtensions.cs  # Message → API model mapping
  Models/<ModelName>.cs         # API response/request records
```

## Endpoint class template

```csharp
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Extensions;
using FunkArr.Core;
using FunkArr.Messages.<Domain>;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ApiModels = FunkArr.Api.Models;

namespace FunkArr.Api;

public static class <Domain>ApiEndpoints
{
    private static readonly TimeSpan _askTimeout = TimeSpan.FromSeconds(10);

    public static WebApplication Map<Domain>Api(this WebApplication app)
    {
        var group = app.MapGroup("/api/<resource>")
            .WithTags("<Tag>")
            .AddEndpointFilter<EndpointExceptionFilter>();

        // endpoints here

        return app;
    }
}
```

## Endpoint patterns

### Simple query (inline lambda)

```csharp
group.MapGet("/", async (IActorRegistry registry) =>
{
    var actor = await registry.GetAsync<IMarkerInterface>();
    var result = await actor.Ask<ResponseType>(new QueryMessage(), _askTimeout);
    return Results.Ok(result.ToApi());
})
.WithSummary("Short description")
.Produces<ApiModels.ResponseModel>()
.ProducesProblem(504);
```

### Query with not-found (response pattern matching)

```csharp
group.MapGet("/{id}", async (string id, IActorRegistry registry) =>
{
    var actor = await registry.GetAsync<IMarkerInterface>();
    var result = await actor.Ask<DetailResponse>(new QueryDetail(id), _askTimeout);
    return result switch
    {
        DetailResult detail => Results.Ok(detail.ToApi()),
        DetailFailed => Results.NotFound(),
        _ => ApiResults.GatewayTimeout(),
    };
})
.WithSummary("Get item details")
.Produces<ApiModels.DetailModel>()
.ProducesProblem(404)
.ProducesProblem(504);
```

### Mutation (static handler method)

For handlers with validation or multi-step logic, extract to a `private static` method:

```csharp
group.MapPost("/", HandleCreate)
    .WithSummary("Create item")
    .Produces<ApiModels.CreatedResponse>(201)
    .ProducesProblem(400)
    .ProducesProblem(422);
```

```csharp
private static async Task<IResult> HandleCreate(
    ApiModels.CreateRequest request, IActorRegistry registry)
{
    // validation → Results.BadRequest(new ApiModels.ErrorResponse("..."))
    // actor ask
    // success → Results.Created($"/api/<resource>/{id}", response)
}
```

### Delete

```csharp
group.MapDelete("/{id}", async (string id, IActorRegistry registry) =>
{
    var actor = await registry.GetAsync<IMarkerInterface>();
    var result = await actor.Ask<DeleteResponse>(new DeleteItem(id), _askTimeout);
    return result.Success
        ? Results.Ok(new ApiModels.OperationResult(true))
        : Results.NotFound(new ApiModels.OperationResult(false, result.Error));
})
.WithSummary("Delete item")
.Produces<ApiModels.OperationResult>()
.ProducesProblem(504);
```

## Actor resolution

- Get actor ref: `await registry.GetAsync<IMarkerInterface>()` — marker interfaces live in `FunkArr.Core`
- Ask with timeout: `actor.Ask<ResponseType>(message, _askTimeout)`
- Fire-and-forget: `actor.Tell(message)` (rare, only when no response needed)
- Multiple actors: resolve each separately, use `Task.WhenAll` for parallel asks

## Response pattern matching

Always use exhaustive switch with wildcard for timeout:

```csharp
result switch
{
    SuccessType success => Results.Ok(success.ToApi()),
    FailedType => Results.NotFound(),       // or BadRequest, Conflict, etc.
    _ => ApiResults.GatewayTimeout(),        // actor didn't respond in time
};
```

`ApiResults.GatewayTimeout()` returns `Results.Problem(statusCode: 504, title: "Gateway Timeout")`.

## API models

- Namespace: `FunkArr.Api.Models` (aliased as `ApiModels` in endpoint files)
- All models are `sealed record` types
- Request models: `<Name>Request` (e.g., `CreateRuleSetRequest`)
- Response models: `<Name>Response` or descriptive name (e.g., `DownloadQueueResponse`, `OperationResult`)
- Error models: `ErrorResponse(string Message)`, `ValidationErrorResponse(IReadOnlyList<string> Errors)`
- Enums for API-specific status values (e.g., `QueueStatus`, `SourceType`)

## Mapping extensions

- File: `Extensions/<Domain>MappingExtensions.cs`
- Class: `internal static class <Domain>MappingExtensions`
- Methods: `internal static ApiModels.<Model> ToApi(this <MessageType> msg)` — extension on the message/result type
- Reverse: `internal static <MessageType> ToMessage(this ApiModels.<RequestModel> model)` — for request→command conversion
- Keep mapping logic in extensions, not in endpoint handlers

```csharp
using FunkArr.Messages.<Domain>;
using ApiModels = FunkArr.Api.Models;

namespace FunkArr.Api.Extensions;

internal static class <Domain>MappingExtensions
{
    internal static ApiModels.<Model> ToApi(this <ResultType> msg) =>
        new(msg.Field1, msg.Field2, ...);
}
```

## OpenAPI metadata

Every endpoint must have:
- `.WithSummary("...")` — short action description
- `.Produces<T>()` or `.Produces(statusCode)` — success type
- `.ProducesProblem(504)` — always (actor timeout)
- `.ProducesProblem(404)` — when not-found is possible
- `.ProducesProblem(400)` / `.ProducesProblem(422)` — for validation

Optional:
- `.WithDescription("...")` — longer explanation when summary isn't enough
- `.CacheOutput("PolicyName")` — for cacheable list endpoints
- `.ExcludeFromDescription()` — for SSE streams or internal endpoints

## Registration

Register the endpoint group in `Program.cs` or startup:

```csharp
app.Map<Domain>Api();
```

## Checklist

- [ ] Endpoint class: `static [partial] class`, `Map<Domain>Api` extension method
- [ ] Group: `/api/<resource>`, `.WithTags()`, `.AddEndpointFilter<EndpointExceptionFilter>()`
- [ ] Actor resolution via `IActorRegistry.GetAsync<IMarker>()`
- [ ] Response switch with `_ => ApiResults.GatewayTimeout()`
- [ ] API models as `sealed record` in `Models/`
- [ ] Mapping extensions in `Extensions/<Domain>MappingExtensions.cs`
- [ ] OpenAPI: `.WithSummary()`, `.Produces<T>()`, `.ProducesProblem(504)`
- [ ] Registered in startup: `app.Map<Domain>Api()`
