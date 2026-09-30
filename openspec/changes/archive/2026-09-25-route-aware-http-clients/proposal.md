## Why

The network routing implementation (change `network-routing-and-proxy`) added proxy support by manually creating `HttpClientHandler` instances with `WebProxy` inside `SubtitlePreparer.CreateClient()`. This workaround bypasses `IHttpClientFactory` for proxy requests, which means those requests get no resilience policies (retry, timeout, circuit breaker) and no connection pooling. The proxy-aware path and the direct path have different reliability characteristics, which is a bug waiting to happen.

## What Changes

- Register named `HttpClient` instances per route at startup (e.g. `"route:Direct"`, `"route:Austria"`) with the correct proxy handler and standard resilience policies
- Replace `SubtitlePreparer`'s manual `HttpClientHandler` workaround with `httpClientFactory.CreateClient($"route:{routeName}")`
- Thread `routeName` through the download pipeline alongside `proxyUrl` (FFmpeg still needs the raw proxy URL as a CLI argument)
- Centralize proxy-to-HttpClient mapping in DI registration instead of business code

## Capabilities

### New Capabilities

_(none - refactoring existing implementation)_

### Modified Capabilities

- `subtitle-preparer`: Replace `proxyUrl` parameter with `routeName`; use named HttpClient from factory instead of manual handler creation
- `download-worker`: Carry `routeName` alongside `proxyUrl` through download lifecycle

## Impact

- **FunkArr.Download**: `DownloadServiceExtensions` registers named clients per route; `SubtitlePreparer` simplified; `IRemuxer`/`Remuxer` gains `routeName` parameter; `DownloadWorker` stores `routeName`
- **FunkArr.Messages**: `InitDownload` gains `RouteName` parameter
- **FunkArr.Core**: No changes (RoutingOptions already exists)
- **FunkArr (Host)**: `DownloadSetupContainer` passes `IConfiguration` to `AddDownloadServices` so it can read routes
