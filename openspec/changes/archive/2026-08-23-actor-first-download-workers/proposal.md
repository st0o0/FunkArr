# Actor-First Download Workers

## Problem

The download pipeline has a split personality: Worker actors exist only as thin
wrappers that delegate all logic to injected `*Service` classes. This creates:

1. **Dead indirection** — every Worker follows the same template: `Self.Tell(DoWork)`,
   call service, tell parent, stop self. Zero actor-specific logic.
2. **Service layer has no reason to exist** — `Mp4DownloadService`, `HlsDownloadService`,
   `SubtitleAcquisitionService`, `SubtitleNormalizerService`, and `MuxingService` are
   stateless wrappers around HttpClient/Process calls that would live more naturally
   inside the actors that manage those lifecycles.
3. **Wasted Actor model** — lifecycle (PostStop = kill process), supervision, and
   cancellation-via-stop are not exploited. Instead, CancellationTokens and
   try/catch manually replicate what actors give for free.
4. **Inconsistent message naming** — `DownloadRequestTracker` uses CRUD-style commands
   (`CreateRequest`, `UpdateStatus`, `MarkCompleted`) instead of domain-imperative
   naming. Responses use generic suffixes (`StatusResponse`) instead of domain nouns.

## Solution

Eliminate the service layer. Move all I/O logic directly into the Worker actors.
Rename messages to a consistent Command (imperative) / Event (past participle)
convention throughout.

## Scope

### In scope

- Delete: `HlsDownloadService`, `Mp4DownloadService`, `SubtitleAcquisitionService`,
  `SubtitleNormalizerService`, `MuxingService`
- Delete: `DownloadRequest`, `DownloadResult` (service DTOs, replaced by actor ctor params)
- Rewrite: All 5 Worker actors to contain the actual I/O logic
- Refactor: `DownloadCoordinator` constructor — no more service injection, workers
  get their dependencies via `DependencyResolver` or direct ctor params
- Rename: All messages to Command/Event convention
- Add: TestKit-based tests for Workers and Coordinator state machine
- Keep: `FfmpegProgressParser`, `DownloadSourceDetector`, `SubtitleNormalizer` (static
  utilities — pure functions, not services)

### Out of scope

- Progress reporting pipeline (tracker→API) — separate concern
- QueueCoordinator changes
- Persistence DTO changes (wire format stays stable)

## Message naming convention

```
Commands (imperative):           Events/Responses (past participle):
─────────────────────────        ────────────────────────────────────
StartDownload                    DownloadStarted (implicit via persist)
CancelDownload                   DownloadCancelled
FetchVideo                       VideoFetched
AcquireSubtitle                  SubtitleAcquired / NoSubtitleFound
ConvertSubtitle                  SubtitleConverted
RemuxVideo                       VideoRemuxed

Tracker:
TrackDownload                    DownloadTracked
ReportProgress                   ProgressReported
CompleteDownload                 DownloadCompleted
FailDownload                     DownloadFailed
QueryStatus                      → DownloadStatus (response)
QueryHistory                     → DownloadHistoryEntry (response)
```

## Design sketch

```
┌─────────────────────────────────────────────────────────────────┐
│  DownloadCoordinator (ReceivePersistentActor, state machine)    │
│                                                                 │
│  Dependencies: IActorRegistry, IFileService                     │
│  NO service injection — workers own their I/O                   │
│                                                                 │
│  WaitingForJob ─── StartDownload ──► Fetching                   │
│                                        │                        │
│  spawns:  HlsDownloadWorker            │ VideoFetched           │
│           OR DirectDownloadWorker      ▼                        │
│                                    AcquiringSubtitle            │
│  spawns:  SubtitleExtractWorker        │ SubtitleAcquired       │
│           OR DirectDownloadWorker      ▼                        │
│                                    ConvertingSubtitle           │
│  spawns:  SubtitleConvertWorker        │ SubtitleConverted      │
│                                        ▼                        │
│                                    Muxing                       │
│  spawns:  RemuxWorker                  │ VideoRemuxed           │
│                                        ▼                        │
│                                    Done                         │
└─────────────────────────────────────────────────────────────────┘

Worker lifecycle (all workers):
┌──────────────────────────────────────────────┐
│  PreStart: allocate resources                │
│  Self.Tell(Execute) → do async I/O           │
│  Success → Tell(Parent, <Result>)            │
│  Failure → Tell(Parent, WorkerFailed)        │
│  PostStop: kill process / dispose resources  │
│  Supervisor: OneForOne → Stop                │
└──────────────────────────────────────────────┘
```

## Worker actor responsibilities

| Worker | Owns | Dependencies |
|--------|------|--------------|
| `HlsDownloadWorker` | Start ffmpeg, stream stderr, report progress, produce video file | IFileService |
| `DirectDownloadWorker` | HTTP GET with streaming write, report progress, produce file | IHttpClientFactory, IFileService |
| `SubtitleExtractWorker` | ffprobe check + ffmpeg subtitle extraction | IFileService |
| `SubtitleConvertWorker` | Call `SubtitleNormalizer.NormalizeAsync` (static) | — |
| `RemuxWorker` | Start ffmpeg mux, await completion, cleanup temps | IFileService |

## Risk & mitigation

| Risk | Mitigation |
|------|-----------|
| Actor-internal async may block mailbox | Use `ReceiveAsync` (already in use) — processes one message at a time per worker, which is fine since each worker handles exactly one job |
| Harder to unit-test I/O logic in isolation | TestKit integration tests cover the actor; static helpers (`BuildFfmpegArgs`, `FfmpegProgressParser`) stay unit-testable |
| Worker constructors get bulky | Use `DependencyResolver.Props<T>()` for DI; pass job-specific params via the initial command message instead of ctor |

## Migration path

1. Rename messages (Coordinator + Tracker) — purely mechanical
2. Inline service logic into each Worker, one at a time
3. Remove service registrations from `FunkArrServiceSetup`
4. Delete service files + `DownloadRequest` / `DownloadResult`
5. Adjust Coordinator to stop injecting services, pass DI-resolved deps to workers
6. Add TestKit tests for each Worker + Coordinator state transitions
