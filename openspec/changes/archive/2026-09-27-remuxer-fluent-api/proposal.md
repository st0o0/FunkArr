## Why

The Remuxer is now a single ~335 LOC class mixing subtitle download, FFmpeg execution, and orchestration. Its public interface still takes 7 loose parameters. DownloadWorker persists RouteName and ProxyUrl even though they're derivable from Channel via IRouteResolver. The code needs clear internal boundaries and a fluent API that callers can extend without signature changes.

## What Changes

- **Add** `RemuxOptions` public record with fluent builder: `RemuxOptions.Create(url, output).WithSubtitle(url).WithChannel(ch).Build()`
- **BREAKING** `IRemuxer.RunAsync` signature changes from 7 parameters to `(RemuxOptions, Action<ProgressUpdate>, CancellationToken)`
- **Split** Remuxer.cs into focused internal classes: `SubtitleDownloader`, `FfmpegProcess`, thin `Remuxer` orchestrator
- **Add** internal interfaces `ISubtitleDownloader` and `IFfmpegProcess` for the split classes
- **Move** `IRouteResolver` into Remuxer (resolves channel to route+proxy internally)
- **BREAKING** `DownloadInitialized` persistence record loses `RouteName` and `ProxyUrl` fields
- **Simplify** `DownloadManager` (no longer resolves route) and `DownloadWorker`/`DownloadWorkerState` (no routeName/proxyUrl in state)

## Capabilities

### New Capabilities

- `remux-options-builder`: Fluent builder API for RemuxOptions with computed IsHls and channel-based routing

### Modified Capabilities

- `remuxer`: IRemuxer signature changes to accept RemuxOptions, Remuxer splits into orchestrator + internal classes with IRouteResolver
- `download-worker`: Builds RemuxOptions instead of passing loose parameters, removes routeName/proxyUrl from state
- `download-manager`: No longer resolves route, passes channel through to DownloadWorker
- `download-messages`: InitDownload command loses routeName/proxyUrl parameters

## Impact

- `src/FunkArr.Download/` - Remuxer.cs split into multiple files, new RemuxOptions.cs, updated IRemuxer.cs, new internal interfaces+classes
- `src/FunkArr.Download/DownloadWorker.cs` - builds RemuxOptions, state loses routeName/proxyUrl
- `src/FunkArr.Download/DownloadWorkerState.cs` - remove RouteName, ProxyUrl fields
- `src/FunkArr.Download/DownloadManager.cs` - remove IRouteResolver dependency, simplify InitDownload
- `src/FunkArr.Download/DownloadServiceExtensions.cs` - register new internal services
- `src/FunkArr.Messages/Download/` - InitDownload, related commands lose route params
- `src/FunkArr.Persistence/Events/Download/DownloadInitialized.cs` - remove RouteName, ProxyUrl (v0.x breaking)
- `src/FunkArr.Download.Tests/` - update tests for new signatures
