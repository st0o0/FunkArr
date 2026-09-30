# Tasks: Actor-First Download Workers

## Phase 1: Message rename

- [x] Rename DownloadCoordinator internal messages: `VideoFetchDone` → `VideoFetched`, `SubtitleAcquireDone` → `SubtitleAcquired`, `NoSubtitleAvailable` → merged into `SubtitleAcquired(NzoId, null)`, `SubtitleConvertDone` → `SubtitleConverted`, `RemuxDone` → `VideoRemuxed`
- [x] Add internal command messages: `FetchVideo`, `AcquireSubtitle`, `ConvertSubtitle`, `RemuxVideo` (Worker triggers)
- [x] Rename DownloadRequestTracker messages: `CreateRequest` → `TrackDownload`, `UpdateStatus` → `ReportProgress`, `MarkCompleted` → `CompleteDownload`, `MarkFailed` → `FailDownload`, `GetStatus` → `QueryStatus`, `GetHistoryEntry` → `QueryHistory`, `StatusResponse` → `DownloadStatus`, `HistoryEntryResponse` → `DownloadHistoryEntry`
- [x] Update all call sites in DownloadCoordinator and QueueCoordinator for renamed tracker messages

## Phase 2: Inline services into workers

- [x] Rewrite `DirectDownloadWorker`: inline `Mp4DownloadService.DownloadAsync` logic, accept `FetchVideo` command, resolve deps via DI
- [x] Rewrite `HlsDownloadWorker`: inline `HlsDownloadService.DownloadAsync` logic, add `PostStop` process kill, accept `FetchVideo` command
- [x] Rewrite `SubtitleExtractWorker`: inline `SubtitleAcquisitionService.AcquireAsync` logic (ffprobe + ffmpeg), accept `AcquireSubtitle` command, add `PostStop` process kill
- [x] Rewrite `SubtitleConvertWorker`: call `SubtitleNormalizer.NormalizeAsync` directly (already static), accept `ConvertSubtitle` command
- [x] Rewrite `RemuxWorker`: inline `MuxingService.MuxAsync` logic, add `PostStop` process kill, accept `RemuxVideo` command, keep metrics recording

## Phase 3: Coordinator refactor

- [x] Remove service fields from `DownloadCoordinator` constructor (keep only `IActorRegistry`, `IFileService`)
- [x] Change worker spawning to use `DependencyResolver.For(Context.System).Props<WorkerType>()` + initial command message
- [x] Update Coordinator state handlers: send command message to spawned worker instead of relying on `Self.Tell(DoWork)` pattern in worker ctor

## Phase 4: Cleanup

- [x] Delete `HlsDownloadService.cs`
- [x] Delete `Mp4DownloadService.cs`
- [x] Delete `SubtitleAcquisitionService.cs` (keep `SubtitleNormalizer.cs` — it's static)
- [x] Delete `SubtitleNormalizerService.cs` (one-line wrapper, pointless)
- [x] Delete `MuxingService.cs` (logic moved to RemuxWorker)
- [x] Delete `DownloadRequest.cs` and `DownloadResult.cs` (service DTOs, replaced by command messages)
- [x] Remove service registrations from `FunkArrServiceSetup.cs`
- [x] Move `MuxOutcome` to `RemuxWorker` or delete if no longer needed (RemuxWorker can just tell `VideoRemuxed` / `WorkerFailed` directly)

## Phase 5: Tests

- [x] Add TestKit test: `DirectDownloadWorker` — mock IHttpClientFactory, verify `VideoFetched` message
- [x] Add TestKit test: `HlsDownloadWorker` — verify `VideoFetched` on success, `WorkerFailed` on process failure
- [x] Add TestKit test: `SubtitleExtractWorker` — verify `SubtitleAcquired` with/without subtitle stream
- [x] Add TestKit test: `SubtitleConvertWorker` — verify `SubtitleConverted` / `WorkerFailed`
- [x] Add TestKit test: `RemuxWorker` — verify `VideoRemuxed` / `WorkerFailed`
- [x] Add TestKit test: `DownloadCoordinator` state machine — full happy path StartDownload → VideoFetched → SubtitleAcquired → SubtitleConverted → VideoRemuxed → Done
- [x] Add TestKit test: `DownloadCoordinator` cancel mid-download
- [x] Add TestKit test: `DownloadRequestTracker` — command/event/query cycle
