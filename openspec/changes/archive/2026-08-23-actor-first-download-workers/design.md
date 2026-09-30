# Design: Actor-First Download Workers

## Core principle

Each Worker actor IS the unit of work. No service delegation. The actor lifecycle
maps 1:1 to the I/O lifecycle: start = PreStart/Execute, cancel = Stop, cleanup = PostStop.

## Worker pattern

All workers follow a single structural pattern:

```csharp
internal sealed class XyzWorker : ReceiveActor
{
    // Command to trigger work (sent by self in ctor)
    private sealed record Execute;

    public XyzWorker(/* DI deps */, /* job params from message */)
    {
        // Store params, setup

        ReceiveAsync<Execute>(async _ =>
        {
            try
            {
                // Do the actual I/O work
                // Report result to parent
                Context.Parent.Tell(new SomethingDone(...));
            }
            catch (Exception ex)
            {
                Context.Parent.Tell(new WorkerFailed(...));
            }
            finally
            {
                Context.Stop(Self);
            }
        });

        Self.Tell(new Execute());
    }

    protected override void PostStop()
    {
        // Kill external process if alive, dispose resources
    }
}
```

## Worker dependency injection

Workers need DI-resolved dependencies (IHttpClientFactory, IFileService). Two options:

**Option A — Pass from Coordinator (current approach):**
Coordinator receives deps via DI, passes them to worker via Props lambda.
Simple but means Coordinator holds references it doesn't use itself.

**Option B — DependencyResolver per Worker:**
Each worker is created via `DependencyResolver.Props<WorkerType>(args)`.
Coordinator only passes job-specific params. Worker gets its own deps from DI.

→ **Decision: Option B.** Coordinator stays lean. Workers declare their own
dependencies. Job-specific params (nzoId, url, paths) come via the initial
command message that triggers work, not via constructor.

## Revised worker init pattern with DI

```csharp
internal sealed class DirectDownloadWorker : ReceiveActor
{
    private sealed record Execute;

    public DirectDownloadWorker(IHttpClientFactory httpClientFactory, IFileService fileService)
    {
        ReceiveAsync<FetchVideo>(async cmd =>
        {
            try
            {
                // cmd carries nzoId, url, tempPath
                var client = httpClientFactory.CreateClient();
                var tempFile = fileService.GetTempVideoPath(cmd.TempPath, cmd.NzoId);
                // ... streaming download logic ...
                Context.Parent.Tell(new VideoFetched(cmd.NzoId, tempFile));
            }
            catch (Exception ex)
            {
                Context.Parent.Tell(new WorkerFailed(cmd.NzoId, Classify(ex), ex.Message));
            }
            finally
            {
                Context.Stop(Self);
            }
        });
    }
}
```

Coordinator spawns:
```csharp
var props = DependencyResolver.For(Context.System).Props<DirectDownloadWorker>();
_currentWorker = Context.ActorOf(props, "direct-video");
_currentWorker.Tell(new FetchVideo(_nzoId, _videoUrl, _tempPath));
```

## Message hierarchy

### DownloadCoordinator messages (top-level, shared)

```
// Public commands (from QueueCoordinator)
StartDownload(NzoId, VideoUrl, SubtitleUrl?, TempPath, OutputDir, Title) : IWithNzoId
CancelDownload(NzoId) : IWithNzoId

// Internal commands (Coordinator → Worker, carry job params)
FetchVideo(NzoId, Url, TempPath) : IWithNzoId
AcquireSubtitle(NzoId, SubtitleUrl?, HlsManifestUrl?, TempPath) : IWithNzoId
ConvertSubtitle(NzoId, SubtitlePath, TempPath) : IWithNzoId
RemuxVideo(NzoId, VideoPath, SubtitlePath?, OutputDir, Title) : IWithNzoId

// Internal events (Worker → Coordinator)
VideoFetched(NzoId, VideoPath) : IWithNzoId
SubtitleAcquired(NzoId, SubtitlePath?) : IWithNzoId
SubtitleConverted(NzoId, NormalizedPath) : IWithNzoId
VideoRemuxed(NzoId, OutputPath) : IWithNzoId
WorkerFailed(NzoId, FailureKind, Reason) : IWithNzoId
```

### DownloadRequestTracker messages (nested in class)

```
// Commands
TrackDownload(NzoId, Title, DownloadUrl, EnqueuedAt) : IWithNzoId
ReportProgress(NzoId, Status) : IWithNzoId
CompleteDownload(NzoId, OutputPath) : IWithNzoId
FailDownload(NzoId, Error) : IWithNzoId
QueryStatus(NzoId) : IWithNzoId
QueryHistory(NzoId) : IWithNzoId

// Responses
DownloadStatus(NzoId, Title, Status, EnqueuedAt)
DownloadHistoryEntry(NzoId, Title, Status, OutputPath?, CompletedAt?, ErrorMessage?)
```

## Coordinator simplification

After removing service injection, the Coordinator constructor becomes:

```csharp
public DownloadCoordinator(IActorRegistry actorRegistry, IFileService fileService)
```

Worker spawning uses DependencyResolver — the Coordinator doesn't know or care
what dependencies its children need.

## PostStop for process-based workers

HlsDownloadWorker and RemuxWorker start external processes (ffmpeg). On cancel:

```csharp
private Process? _process;

protected override void PostStop()
{
    if (_process is { HasExited: false })
    {
        _process.Kill(entireProcessTree: true);
        _process.Dispose();
    }
}
```

This replaces manual CancellationToken + timeout logic. `Context.Stop(Self)` or
`PoisonPill` triggers PostStop → process dies. Clean, deterministic.

## Testing strategy

| Target | Test approach |
|--------|--------------|
| Static helpers (FfmpegProgressParser, BuildFfmpegArgs, ClassifyException) | Plain xUnit unit tests |
| Worker actors | TestKit: create with TestProbe as parent, send command, ExpectMsg result |
| DownloadCoordinator state machine | TestKit: inject TestProbe shards, verify message sequence through stages |
| DownloadRequestTracker | TestKit: verify persistence + query responses |

Worker integration tests can use `TestActorRef` for synchronous assertions when
testing against mocked I/O (IFileService, IHttpClientFactory via NSubstitute).
