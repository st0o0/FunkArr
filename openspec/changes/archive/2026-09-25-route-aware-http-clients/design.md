## Context

The `network-routing-and-proxy` change added proxy support to the download pipeline. SubtitlePreparer currently creates `new HttpClient(new HttpClientHandler { Proxy = new WebProxy(proxyUrl) })` when a proxy is needed, bypassing `IHttpClientFactory`. This means proxy requests miss resilience policies and connection pooling.

Routes are configured at startup via `RoutingOptions` and don't change at runtime (they require a restart). This makes them ideal for named HttpClient registration.

## Goals / Non-Goals

**Goals:**
- All HTTP clients (proxy and direct) go through `IHttpClientFactory` with resilience policies
- Proxy configuration centralized in DI registration, not in business code
- SubtitlePreparer has no knowledge of proxy mechanics

**Non-Goals:**
- Changing FFmpeg proxy handling (FFmpeg is an external process, needs `-http_proxy` CLI arg)
- Dynamic route changes without restart (routes are startup config)
- Routing MVW search through proxies (future work, but this lays the groundwork)

## Decisions

### 1. Named HttpClient per route definition

At startup, `DownloadServiceExtensions.AddDownloadServices` reads `RoutingOptions` and registers one named HttpClient per `RouteDefinition`:

- Client name: `$"route:{definition.Name}"` (e.g. `"route:Direct"`, `"route:Austria"`)
- When `definition.Proxy` is set: `ConfigurePrimaryHandler` returns `HttpClientHandler` with `WebProxy`
- All clients get `AddStandardResilienceHandler` with the same timeout config

This replaces the single `"SubtitlePreparer"` named client.

### 2. routeName flows alongside proxyUrl

The download pipeline needs both:
- `routeName` for HttpClient lookup (SubtitlePreparer)
- `proxyUrl` for FFmpeg CLI argument (FfmpegRunner)

Both come from `ResolvedRoute` and are set in `InitDownload`. The Remuxer receives both and passes each to the right consumer.

Alternative considered: have Remuxer look up the proxy URL from RoutingOptions by route name. Rejected because it adds a dependency on RoutingOptions to Remuxer, and the proxyUrl is already resolved at dispatch time.

### 3. SubtitlePreparer takes routeName, not proxyUrl

The interface changes from `PrepareAsync(url, outputDir, proxyUrl, ct)` to `PrepareAsync(url, outputDir, routeName, ct)`. The implementation becomes a one-liner: `httpClientFactory.CreateClient($"route:{routeName}")`. No more `System.Net` import, no more `WebProxy`, no more manual handler creation.

## Risks / Trade-offs

- [Route added to config but app not restarted] -> Named clients are registered at startup only. New routes require a restart. This matches the existing behavior (RoutingOptions are read via IOptionsMonitor but HttpClient registrations are immutable after startup).
- [Route name mismatch] -> If `routeName` in a message doesn't match a registered client name, `CreateClient` returns an unconfigured default client (no proxy). This is the same behavior as the current default fallback. Validation at startup ensures all route references are valid.
