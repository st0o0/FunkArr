## 1. Named HttpClient Registration

- [x] 1.1 Update `DownloadServiceExtensions.AddDownloadServices` to accept `IConfiguration` and register a named HttpClient per `RouteDefinition` from `RoutingOptions` (name: `$"route:{def.Name}"`, with `ConfigurePrimaryHandler` using `WebProxy` when `def.Proxy` is set, plus `AddStandardResilienceHandler`)
- [x] 1.2 Remove the old `"SubtitlePreparer"` named client registration
- [x] 1.3 Update `DownloadSetupContainer` to pass `configuration` to `AddDownloadServices`

## 2. Message and Interface Changes

- [x] 2.1 Add `RouteName` (string) parameter to `InitDownload` message (alongside existing `ProxyUrl`)
- [x] 2.2 Update `ISubtitlePreparer.PrepareAsync` signature: replace `proxyUrl` with `routeName`
- [x] 2.3 Update `IRemuxer.RunAsync` signature: replace `proxyUrl` with `routeName` + `proxyUrl` (needs both)

## 3. Implementation Changes

- [x] 3.1 Simplify `SubtitlePreparer`: remove `CreateClient` method, use `httpClientFactory.CreateClient($"route:{routeName}")` directly
- [x] 3.2 Update `Remuxer.RunAsync`: pass `routeName` to `SubtitlePreparer`, pass `proxyUrl` to `FfmpegRunner`
- [x] 3.3 Update `DownloadWorker`: store `RouteName` from `InitDownload` in-memory alongside `ProxyUrl`, pass both through to `IRemuxer.RunAsync`
- [x] 3.4 Update `DownloadManager.HandleAdd`: pass `route.Name` as `RouteName` in `InitDownload`

## 4. Tests

- [x] 4.1 Update existing `DownloadManager` tests to include `RouteName` in constructor/message setup
- [x] 4.2 Update `FfmpegRunner.BuildArguments` tests (no change expected, verify they still pass)
- [x] 4.3 Run all test projects to verify no regressions
