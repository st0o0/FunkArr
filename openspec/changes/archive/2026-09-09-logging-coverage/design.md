# Design - Logging Coverage

## Pattern

All actors already have the established pattern from the RuleSet domain:

```csharp
private readonly ILoggingAdapter _log = Context.GetLogger();
```

This is the Akka standard. Serilog integration is already configured via `Akka.Logger.Serilog` and `AddLoggerFactory()` in `AkkaSetupContainer`, so `ILoggingAdapter` output flows through Serilog and gets the same enrichment and formatting as `ILogger<T>`.

For non-actor DI services, inject `ILogger<T>` via constructor (same as `DataFiles`).

## Level conventions

| Level | Use for | Examples |
|---|---|---|
| Info | Lifecycle events, significant state changes | "Download {Id} started", "Search completed with {Count} results" |
| Warning | Recoverable failures, unexpected but handled | "TVDB auth failed, retrying", "Snapshot save failed", "HTTP 429 from MediathekViewWeb" |
| Error | Unrecoverable failures | Already used correctly in RuleSetUpdater |
| Debug | Internal state, diagnostics | "Cache hit for {TmdbId}", "Queue depth: {Count}", "Stashing - at capacity" |

## What to log per domain

### Download
- **DownloadManager**: Info on enqueue/dispatch/complete/fail. Debug on queue depth after changes.
- **DownloadWorker**: Info on start/success/fail with download ID and title. Warning on subtitle fallback. Warning on empty video URL fault.

### Search
- **SearchManager**: Warning on search timeout.
- **TvSearchWorker**: Info on search start. Warning on MediathekQueryFailed, RuleSetNotFound, EpisodeResolutionFailed, Status.Failure.
- **MovieSearchWorker**: Same pattern as TvSearchWorker.
- **MediathekViewWebManager**: Warning on HttpFailed. Debug on stash (at capacity).

### MetadataResolver
- **MetadataResolverManager**: Debug on cache hit/miss. Info on resolve request forwarded.
- **TmdbResolverActor**: Add Info on fetch start (TvdbResolverActor already has this).

### MatchMagic
- **MatchHistoryWorker**: Warning on SaveSnapshotFailure (currently swallowed silently).
- **MatchMagicManager**: Debug on config lookup miss.

### Non-actor services
- **TvdbClient**: Warning on auth failure, HTTP non-success. Debug on successful auth.
- **TmdbClient**: Warning on HTTP non-success with URL and status code.

## Structured logging

Use Serilog message templates consistently (already the pattern in RuleSet):

```csharp
_log.Info("Download {DownloadId} started: {Title}", id, title);
_log.Warning("TVDB auth failed: {StatusCode}", (int)response.StatusCode);
```

No string interpolation in log calls. Property names in PascalCase.
