# Design - Unified HttpClient Handling

## Typed clients for DI services

`TvdbClient` and `TmdbClient` are plain DI services - they fit the typed client pattern perfectly:

```csharp
// ServiceSetupContainer - registration becomes the single source of truth
services.AddHttpClient<TvdbClient>(client =>
{
    client.BaseAddress = new Uri("https://api4.thetvdb.com/v4/");
});

services.AddHttpClient<TmdbClient>(client =>
{
    client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
});
```

The typed client receives a fresh `HttpClient` via constructor injection. The factory manages handler lifetime automatically.

```csharp
// TvdbClient - no more IHttpClientFactory, no more manual BaseAddress
public sealed class TvdbClient(HttpClient httpClient, IOptionsMonitor<TvdbOptions> options)
{
    // httpClient.BaseAddress already set by factory
}
```

## Actors keep IHttpClientFactory

Actors have Akka-managed lifetimes, not DI-managed. They can't be typed clients. Instead they keep `IHttpClientFactory` but call `CreateClient()` per-operation instead of caching:

```csharp
// MediathekViewWebManager - create per request
public MediathekViewWebManager(IHttpClientFactory httpClientFactory, int maxConcurrent = 3)
{
    _httpClientFactory = httpClientFactory; // store factory, not client
    // ...
}

private async Task<HttpResponseMessage> ExecuteQuery(...)
{
    using var client = _httpClientFactory.CreateClient("MediathekViewWeb");
    // ...
}
```

Same for `RuleSetUpdater` - store factory, create client in `DoCheckForUpdates()`.

## Registration consolidation

All 4 clients configured in one place in `ServiceSetupContainer`:

```
AddHttpClient<TvdbClient>        → BaseAddress
AddHttpClient<TmdbClient>        → BaseAddress
AddHttpClient("MediathekViewWeb") → BaseAddress + Accept header
AddHttpClient("GitHub")           → BaseAddress + Accept + User-Agent
```

Named clients remain for actor-consumed ones. Typed clients for DI services.

## Test impact

- `TvdbClient`/`TmdbClient` tests currently create `TestHttpClientFactory` - these switch to passing a plain `HttpClient` directly (typed client constructor takes `HttpClient`)
- `MetadataResolverManagerTests` still uses `IHttpClientFactory` mock (actors still use factory)
- `MediathekViewWebManager` test setup (if any) needs factory mock adjustment

## Decision: per-request vs. per-actor-lifetime

For actors, creating a new `HttpClient` per request is slightly more overhead but ensures proper handler rotation. Given the low request frequency (MediathekViewWeb: throttled to 3 concurrent, RuleSetUpdater: every 30 minutes), per-request is the right call.
