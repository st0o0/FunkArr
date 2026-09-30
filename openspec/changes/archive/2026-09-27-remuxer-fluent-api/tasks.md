## 1. RemuxOptions Builder

- [x] 1.1 Create `RemuxOptions.cs` with public sealed record, `Create(videoUrl, outputPath)` factory, `WithSubtitle(url)`, `WithChannel(channel)`, computed `IsHls`
- [x] 1.2 Update `IRemuxer.cs` signature to `RunAsync(RemuxOptions options, Action<ProgressUpdate> onProgress, CancellationToken ct)`

## 2. Split Remuxer into Internal Classes

- [x] 2.1 Create `ISubtitleDownloader.cs` internal interface with `DownloadAsync(url, outputDirectory, routeName, ct) -> SubtitleResult`
- [x] 2.2 Create `SubtitleDownloader.cs` internal class - extract subtitle download, format detection, SRT conversion, telemetry from Remuxer
- [x] 2.3 Create `FfmpegInput.cs` internal record with `VideoUrl, SubtitlePath, OutputPath, ProxyUrl, SubtitleLanguage, IsHls`
- [x] 2.4 Create `IFfmpegProcess.cs` internal interface with `ExecuteAsync(FfmpegInput, onProgress, ct) -> FfmpegResult`
- [x] 2.5 Create `FfmpegProcess.cs` internal class - extract BuildArguments, process execution, progress parsing, error handling from Remuxer
- [x] 2.6 Rewrite `Remuxer.cs` as thin orchestrator: takes IRouteResolver, ISubtitleDownloader, IFfmpegProcess via DI. Resolves route, calls subtitle downloader, builds FfmpegInput, calls FfmpegProcess, cleans up temp files
- [x] 2.7 Update `DownloadServiceExtensions.cs` to register ISubtitleDownloader, IFfmpegProcess, IRemuxer and add IRouteResolver if not already registered

## 3. Remove Route from Persistence and Messages

- [x] 3.1 Remove `RouteName` and `ProxyUrl` from `DownloadInitialized` persistence event
- [x] 3.2 Remove `RouteName` and `ProxyUrl` from `InitDownload` command (check Messages project)
- [x] 3.3 Remove `RouteName` and `ProxyUrl` from `DownloadWorkerState`
- [x] 3.4 Update `DownloadWorker` to build RemuxOptions with `.WithChannel(media.Channel)` instead of passing routeName/proxyUrl
- [x] 3.5 Update `DownloadManager` to remove IRouteResolver dependency, simplify InitDownload construction
- [x] 3.6 Update `PersistenceMapping.cs` if it maps RouteName/ProxyUrl

## 4. Tests

- [x] 4.1 Add RemuxOptions builder tests (Create, WithSubtitle, WithChannel, IsHls, immutability)
- [x] 4.2 Update RemuxerTests - BuildArguments tests now use FfmpegInput instead of RemuxOptions
- [x] 4.3 Update DownloadWorkerStateTests and DownloadManagerStateTests for removed route fields
- [x] 4.4 Update any other tests referencing routeName/proxyUrl in download contexts

## 5. Verify

- [x] 5.1 Run `dotnet build src/FunkArr.slnx` and fix compilation errors
- [x] 5.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes` and fix formatting
- [x] 5.3 Run all test projects and verify green
