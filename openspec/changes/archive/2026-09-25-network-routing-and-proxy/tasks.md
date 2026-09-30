## 1. Configuration Models and Validation

- [x] 1.1 Add `RoutingOptions`, `RouteDefinition`, `ChannelRoute` classes to FunkArr.Core with `SectionName = "FunkArr:Routes"`
- [x] 1.2 Add `ResolvedRoute` record to FunkArr.Core (Name, ProxyUrl?)
- [x] 1.3 Add `IRouteResolver` interface and `RouteResolver` implementation to FunkArr.Core using `FileSystemName.MatchesSimpleExpression` with first-match-wins
- [x] 1.4 Add `RoutingOptionsValidator` implementing `IValidateOptions<RoutingOptions>` (route references, default, uniqueness, URI format)
- [x] 1.5 Add `RoutingOptions` binding and validation in a setup container (DownloadSetupContainer or new RoutingSetupContainer)
- [x] 1.6 Register `IRouteResolver` as singleton in DI

## 2. Configuration Defaults

- [x] 2.1 Add `Routes` section to `appsettings.json` with a single Direct route definition and Direct as default
- [x] 2.2 Add commented route configuration examples to `docker-compose.example.yml` (Direct, Austria, Switzerland definitions + ORF*/SRF* channel routes)

## 3. Download Messages

- [x] 3.1 Add optional `ProxyUrl` (string?) parameter to `InitDownload` message
- [x] 3.2 Update `AddDownload` or the dispatch path in DownloadManager to resolve the channel's route via `IRouteResolver` and pass `ProxyUrl` into `InitDownload`

## 4. FFmpeg Runner

- [x] 4.1 Add `string? proxyUrl` parameter to `FfmpegRunner.RunAsync` and `BuildArguments`
- [x] 4.2 Inject `-http_proxy {proxyUrl}` via `WithCustomArgument` on `FromUrlInput` options when proxyUrl is non-null
- [x] 4.3 Update `IFfmpegRunner` interface to include the new parameter

## 5. Subtitle Preparer

- [x] 5.1 Add `string? proxyUrl` parameter to `ISubtitlePreparer.PrepareAsync`
- [x] 5.2 Create proxy-aware `HttpClientHandler` with `WebProxy` when proxyUrl is provided
- [x] 5.3 Update `SubtitlePreparer` to use `IHttpClientFactory.CreateClient` with proxy handler per-request

## 6. Remuxer

- [x] 6.1 Add `string? proxyUrl` parameter to `IRemuxer.RunAsync`
- [x] 6.2 Pass proxyUrl through to `SubtitlePreparer.PrepareAsync` and `FfmpegRunner.RunAsync`

## 7. Download Worker

- [x] 7.1 Store `ProxyUrl` from `InitDownload` as in-memory field (not persisted in state or events)
- [x] 7.2 Pass stored `ProxyUrl` to `IRemuxer.RunAsync` in `StartDownload` handler
- [x] 7.3 On recovery, ProxyUrl defaults to null (re-resolved on next InitDownload from DownloadManager)

## 8. Tests

- [x] 8.1 Unit tests for `RouteResolver` (pattern matching, first-match-wins, default fallback, no-config default)
- [x] 8.2 Unit tests for `RoutingOptionsValidator` (valid config, invalid references, duplicate names, bad URIs)
- [x] 8.3 Unit tests for `FfmpegRunner.BuildArguments` with proxy URL (verify `-http_proxy` placement before `-i`)
- [x] 8.4 Update existing `DownloadWorker` tests to pass ProxyUrl through InitDownload
- [x] 8.5 Run all test projects to verify no regressions
