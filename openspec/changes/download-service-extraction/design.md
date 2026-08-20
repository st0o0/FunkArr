# Design: download-service-extraction

## Context

`DownloadQueueActor` (476 lines) mixes three concerns: the persistent state
machine (event sourcing, job bookkeeping), the Akka.Streams pipeline
orchestration (queue, kill switch, supervision, materialization), and raw
HTTP download I/O (`DownloadFilesAsync`, lines 195-245). The I/O method has
no dependency on actor state or `IActorRef` beyond using `self.Tell` to push
progress — it is pure `IHttpClientFactory` + `IFileService` work wrapped in
an actor method only because it was written inline during the original
`streaming-download-pipeline` change.

This blocks two things:
- **Unit testing download I/O** — today it can only be exercised through
  `TestKit`-based actor tests (materializing a real stream, driving the
  actor through `Recovering` → `Materializing` → `Ready`), which is slow and
  indirect for what is fundamentally "GET a URL, write chunks to a file."
- **Future download strategy changes** (HLS variants, resumable downloads,
  retry-with-backoff policies) are currently only reachable by editing the
  actor, mixing stream-lifecycle risk into what should be an isolated I/O
  change.

`MuxingActor` → `MuxingService` (the sibling extraction from
`streaming-download-pipeline`) is the precedent this change follows: the
actor keeps stream orchestration, a plain DI service owns the I/O, and the
stream lambda becomes a thin call into the service.

## Goals / Non-Goals

**Goals**
- Move all HTTP download and subtitle-fetch I/O out of
  `DownloadQueueActor` into a plain, DI-registered `DownloadService`.
- Keep the Akka.Streams pipeline (queue, kill switch, supervision,
  `SelectAsyncUnordered` stages, re-materialization on failure) unchanged in
  shape — it stays in the actor and calls the service instead of inlining
  I/O.
- Make `DownloadService` testable with a mocked `HttpMessageHandler` and a
  fake/mock `IFileService`, with no `ActorSystem` or `TestKit` involved.
- Preserve existing behavior exactly: same progress cadence (every 2s), same
  subtitle fallback semantics (failure → `null`, no exception), same temp
  file naming via `IFileService`.

**Non-Goals**
- Not changing the stream pipeline's shape, concurrency model, supervision
  decider, or kill-switch/re-materialization behavior — those stay as
  specified in `download-pipeline` and `stream-supervision`.
- Not adding retry policies, resumable downloads, or HLS support. This
  change only relocates existing logic to make such follow-ups tractable
  later.
- Not changing `DownloadJob`, `DownloadEvents`, or the persistence DTOs.
- Not changing how progress is applied to actor state
  (`HandleProgressUpdate`) — only how the progress event reaches the actor's
  mailbox.

## Decisions

### Decision: `DownloadService` is a plain class, not an actor

`DownloadService` is registered as `AddSingleton<DownloadService>()` (or
`AddTransient` — see Risks) in `FunkArrServiceSetup`, constructed with
`IHttpClientFactory` and `IFileService`, both already DI-registered
singletons. It has no mutable state and no mailbox; every call is a
self-contained `Task`.

This mirrors `MuxingService`: I/O-heavy, stateless, invoked from inside a
stream stage lambda (`SelectAsyncUnordered`) that already runs off the
actor's thread. Making it an actor would add message-passing overhead and a
`self.Ask` round-trip for what is already an `await`-based async call inside
the stream stage — no benefit, only latency and a second supervision
concern to reason about.

### Decision: Progress reporting via `Action<long, long>` callback, not `IActorRef`

`DownloadService.DownloadAsync` accepts a callback:

```csharp
Task<(string VideoPath, string? SubtitlePath)> DownloadAsync(
    DownloadRequest request,
    Action<long, long> onProgress,
    CancellationToken cancellationToken = default);
```

`onProgress` is invoked with `(downloadedBytes, totalBytes)` at most once
every 2 seconds during the video download loop — the same cadence as today.

The alternative — passing `IActorRef self` and calling `self.Tell(...)`
directly inside the service — was rejected because it would give a supposedly
pure I/O service a compile-time dependency on Akka.NET's actor API and on
`DownloadEvents.DownloadProgressUpdated` specifically. A callback keeps
`DownloadService` framework-agnostic and trivially testable (tests just pass
a lambda that appends to a list). The actor's stream lambda supplies:

```csharp
onProgress: (downloaded, total) =>
    self.Tell(new DownloadEvents.DownloadProgressUpdated(req.NzoId, downloaded, total))
```

This is the same shape `MuxingService` uses conceptually — the service
returns a plain result/outcome, the actor is the only place that knows about
`DownloadEvents` and `self.Tell`.

### Decision: `DownloadRequest` becomes a standalone record

Today `DownloadRequest` is `internal sealed record DownloadRequest(...)`
nested inside `DownloadQueueActor`. Since `DownloadService.DownloadAsync`
takes a `DownloadRequest` as its primary input, the type must be visible
outside the actor. It moves to its own file,
`DownloadClient/DownloadRequest.cs`, as a standalone `public sealed record`
in the `FunkArr.DownloadClient` namespace — same namespace, same field
shape (`NzoId`, `VideoUrl`, `SubtitleUrl`, `TempPath`, `OutputDir`, `Title`),
no behavior change. `DownloadQueueActor` keeps constructing it exactly as
before (`OfferToQueue`); only the declaration site moves.

### Decision: Actor's stream lambda shrinks to a delegating call

`MaterializeStream`'s first `SelectAsyncUnordered` stage currently is:

```csharp
self.Tell(new DownloadEvents.DownloadStarted(req.NzoId));
var (videoPath, subtitlePath) = await DownloadFilesAsync(req, self);
self.Tell(new DownloadEvents.DownloadCompleted(req.NzoId, videoPath, subtitlePath));
return (DownloadOutcome)new DownloadOutcome.Success(req.NzoId, videoPath, subtitlePath);
```

After extraction, `DownloadFilesAsync` is deleted from the actor entirely
and the lambda becomes:

```csharp
self.Tell(new DownloadEvents.DownloadStarted(req.NzoId));
var (videoPath, subtitlePath) = await _downloadService.DownloadAsync(
    req,
    onProgress: (downloaded, total) =>
        self.Tell(new DownloadEvents.DownloadProgressUpdated(req.NzoId, downloaded, total)),
    cancellationToken: default);
self.Tell(new DownloadEvents.DownloadCompleted(req.NzoId, videoPath, subtitlePath));
return (DownloadOutcome)new DownloadOutcome.Success(req.NzoId, videoPath, subtitlePath);
```

The surrounding `try`/`catch` that converts exceptions into
`DownloadOutcome.Failure` and the `DownloadEvents.DownloadFailed` tell stays
in the actor unchanged — exception-to-event translation is orchestration,
not I/O, and stays where `DownloadOutcome` is defined and consumed.

`DownloadQueueActor` gains a constructor dependency on `DownloadService` and
drops direct use of `IHttpClientFactory` (the actor no longer calls
`_httpClientFactory.CreateClient()` itself). `IFileService` stays on the
actor because `PreStart` still calls `EnsureDirectoriesExist`.

### Decision: Subtitle fallback logic moves as-is

The existing behavior — attempt subtitle GET, on non-success log a warning
and return `null` instead of throwing — moves into `DownloadService`
unchanged. `DownloadService` takes an `ILogger<DownloadService>` (via
standard DI logging, since it is a plain class and has no
`Context.GetLogger()` available) to replace the actor's `ILoggingAdapter`
for this one warning. This is the only new dependency `DownloadService`
needs beyond what `DownloadFilesAsync` already used.

## Risks / Trade-offs

- **Progress callback runs on the stream's async I/O thread, not the actor's
  thread.** `self.Tell` is thread-safe (mailboxes accept sends from any
  thread), so this is safe today and stays safe after extraction — but it
  means `onProgress` MUST stay a lightweight, non-blocking call. The
  contract documented on `DownloadService.DownloadAsync` should state this
  explicitly: `onProgress` must not block or throw; a throwing callback
  would propagate out of the download loop and fail the whole download,
  which is what happens today. No new risk is introduced — this fixes the
  ambiguity as an explicit design constraint rather than the previous
  implicit `self.Tell` behavior.
- **Singleton vs. transient lifetime.** `DownloadService` holds no mutable
  state, so singleton is safe and matches `MuxingService`'s registration.
  If this changes later (e.g., adding internal caching), the registration
  will need revisiting — flagged here so it isn't silently forgotten.
- **`CancellationToken` is currently unused by the actor** (stream stages
  don't thread a token today). `DownloadAsync` accepts one for future use
  and testability (tests can assert cancellation mid-download), but the
  actor passes `CancellationToken.None`/`default` until a follow-up change
  wires real cancellation through the kill switch. This is a deliberate
  scope boundary, not an oversight — see Non-Goals.
- **Test double surface area.** `DownloadQueueActorTests` currently
  constructs the actor with a real or faked `IHttpClientFactory`. After this
  change those tests must inject a `DownloadService` (real, backed by a
  mocked `HttpMessageHandler`, or a hand-rolled test double implementing the
  same shape) instead. This is called out explicitly in tasks.md so it isn't
  missed during implementation.
