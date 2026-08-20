## 1. DownloadRequest relocation

- [ ] 1.1 Create `DownloadClient/DownloadRequest.cs` with `public sealed record DownloadRequest(string NzoId, string VideoUrl, string? SubtitleUrl, string TempPath, string OutputDir, string Title)` — same field shape as the current nested `internal sealed record`
- [ ] 1.2 Remove the nested `internal sealed record DownloadRequest` from `DownloadQueueActor`
- [ ] 1.3 Verify `OfferToQueue` and `MaterializeStream` in `DownloadQueueActor` still compile against the standalone type (no field/usage changes expected)

## 2. Create DownloadService

- [ ] 2.1 Create `DownloadClient/DownloadService.cs` — `public sealed class DownloadService(IHttpClientFactory httpClientFactory, IFileService fileService, ILogger<DownloadService> logger)`
- [ ] 2.2 Implement `Task<(string VideoPath, string? SubtitlePath)> DownloadAsync(DownloadRequest request, Action<long, long> onProgress, CancellationToken cancellationToken = default)`
- [ ] 2.3 Port the video download loop from `DownloadQueueActor.DownloadFilesAsync` (lines 195-225): `GetAsync` with `ResponseHeadersRead`, `EnsureSuccessStatusCode`, 8192-byte chunked read/write, 2-second progress cadence via `onProgress` instead of `self.Tell`
- [ ] 2.4 Port the subtitle download block (lines 227-242): fallback to `null` + warning log on non-success, using `ILogger<DownloadService>` instead of `ILoggingAdapter`
- [ ] 2.5 Thread `cancellationToken` through the `GetAsync`, `ReadAsStreamAsync`/`ReadAsync`, and `WriteAsync` calls

## 3. Update DownloadQueueActor

- [ ] 3.1 Add `DownloadService` constructor dependency to `DownloadQueueActor`; remove direct `IHttpClientFactory` field/usage (keep `IFileService` — still needed for `EnsureDirectoriesExist` in `PreStart`)
- [ ] 3.2 Delete `DownloadFilesAsync` from `DownloadQueueActor`
- [ ] 3.3 Update the first `SelectAsyncUnordered` stage in `MaterializeStream` to call `_downloadService.DownloadAsync(req, onProgress: (downloaded, total) => self.Tell(new DownloadEvents.DownloadProgressUpdated(req.NzoId, downloaded, total)))`, keeping the existing `try`/`catch` → `DownloadOutcome`/`DownloadEvents.DownloadFailed` translation unchanged
- [ ] 3.4 Confirm `DownloadEvents.DownloadStarted`/`DownloadCompleted` tells stay in the actor's stream lambda, not the service

## 4. DI registration

- [ ] 4.1 Register `services.AddSingleton<DownloadService>();` in `FunkArrServiceSetup` alongside `MuxingService`

## 5. Tests

- [ ] 5.1 Write `DownloadServiceTests` with a mocked `HttpMessageHandler` (via `HttpClient(handler)` + `IHttpClientFactory` stub) and a fake/mock `IFileService`: successful video download writes chunks and returns temp path
- [ ] 5.2 Test subtitle success path — subtitle written via `IFileService.WriteSubtitleAsync`, returned path non-null
- [ ] 5.3 Test subtitle failure path — non-success status returns `SubtitlePath = null`, no exception thrown, warning logged
- [ ] 5.4 Test no-subtitle path — `SubtitleUrl = null` results in zero subtitle HTTP calls
- [ ] 5.5 Test progress callback invocation — verify `onProgress` receives cumulative byte counts (use a small buffered stream and a short/zeroed cadence or directly assert loop behavior without relying on real 2-second waits)
- [ ] 5.6 Test cancellation — cancelling the token mid-download causes `DownloadAsync` to fault/cancel rather than complete
- [ ] 5.7 Test video HTTP failure — non-success status on `VideoUrl` throws, no dangling open file handle
- [ ] 5.8 Update `DownloadQueueActorTests` (or equivalent actor-level tests) to inject a `DownloadService` instance instead of relying on `IHttpClientFactory` wiring directly on the actor
- [ ] 5.9 Run full test suite (`dotnet run --project FunkArr.Tests/FunkArr.Tests.csproj`) and verify all existing tests still pass

## 6. Build verification

- [ ] 6.1 Run `dotnet build FunkArr.slnx` from `src/` and confirm no warnings/errors
- [ ] 6.2 Confirm `DownloadFilesAsync` is fully removed and no dead references to the old nested `DownloadRequest` remain (search for `DownloadQueueActor.DownloadRequest`)
