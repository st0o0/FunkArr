## Context

The project-cleanup change consolidated FfmpegRunner and SubtitlePreparer into Remuxer. This follow-up splits the monolithic Remuxer into focused classes with clean internal boundaries, introduces a fluent RemuxOptions builder as the public API, and moves route resolution into the Remuxer so callers don't need to deal with proxy details.

## Goals / Non-Goals

**Goals:**
- Fluent builder API for RemuxOptions that callers construct
- IRemuxer.RunAsync accepts a single options object instead of 7 parameters
- Clear internal split: subtitle download, FFmpeg execution, orchestration
- Route resolution moves into Remuxer (channel in, route resolved internally)
- Remove redundant RouteName/ProxyUrl from persistence and worker state

**Non-Goals:**
- No changes to subtitle format classes or parsing logic
- No changes to IRouteResolver or RoutingOptions
- No changes to DownloadScheduler or DownloadHistoryManager
- No new public interfaces beyond IRemuxer and RemuxOptions

## Decisions

### 1. RemuxOptions as immutable record with static builder methods

**Decision:** RemuxOptions is a `public sealed record` with `Create()` factory and `With*()` methods that return new instances.

```csharp
public sealed record RemuxOptions
{
    public string VideoUrl { get; init; }
    public string OutputPath { get; init; }
    public string? SubtitleUrl { get; init; }
    public string? Channel { get; init; }
    public bool IsHls => VideoUrl.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase);

    private RemuxOptions(string videoUrl, string outputPath) { ... }

    public static RemuxOptions Create(string videoUrl, string outputPath)
        => new(videoUrl, outputPath);

    public RemuxOptions WithSubtitle(string? url)
        => this with { SubtitleUrl = url };

    public RemuxOptions WithChannel(string? channel)
        => this with { Channel = channel };
}
```

**Why:** Records give immutability and `with` expressions for free. No separate builder class needed. The `Create` factory enforces required params (videoUrl, outputPath), optional params are added via `With*` methods. Extending later (e.g. `WithLanguage`, `WithTimeout`) requires zero breaking changes.

### 2. Internal split: SubtitleDownloader + FfmpegProcess + Remuxer

**Decision:** Three internal classes, each with its own internal interface:

| Class | Responsibility | Interface |
|-------|---------------|-----------|
| `SubtitleDownloader` | HTTP download, format detection, SRT conversion, telemetry | `ISubtitleDownloader` |
| `FfmpegProcess` | BuildArguments, process execution, progress parsing, error handling | `IFfmpegProcess` |
| `Remuxer` | Orchestration: resolve route, prepare subtitle, run FFmpeg, cleanup | `IRemuxer` (public) |

**Why:** Each class has a single concern. Internal interfaces enable DI registration and testability without exposing implementation to other projects. The Remuxer becomes a thin orchestrator (~40 LOC).

### 3. FfmpegProcess uses an internal FfmpegInput record

**Decision:** FfmpegProcess.ExecuteAsync accepts an internal `FfmpegInput` record built by the Remuxer after subtitle preparation and route resolution:

```csharp
internal sealed record FfmpegInput(
    string VideoUrl, string? SubtitlePath, string OutputPath,
    string? ProxyUrl, string? SubtitleLanguage, bool IsHls);
```

**Why:** FfmpegProcess doesn't need to know about channels or subtitle URLs - it only needs resolved paths and proxy. This keeps the FFmpeg layer independent of routing and subtitle download concerns.

### 4. Remuxer owns IRouteResolver

**Decision:** Remuxer takes `IRouteResolver` via DI and resolves `channel -> (routeName, proxyUrl)` internally. The resolved route is used for both FFmpeg proxy and subtitle HttpClient.

**Why:** Route resolution is a remux concern, not a download orchestration concern. The DownloadManager currently resolves routes and passes them through persistence into the worker state. Moving resolution into Remuxer removes two persisted fields and simplifies the entire chain.

### 5. Remove RouteName/ProxyUrl from persistence (v0.x breaking)

**Decision:** Remove `RouteName` and `ProxyUrl` from `DownloadInitialized` persistence event and `DownloadWorkerState`. Channel (already in `PersistedDownloadMedia`) is sufficient to derive the route at runtime.

**Why:** Version 0.x allows breaking changes. The fields are redundant - `IRouteResolver.Resolve(channel)` produces the same result. Removing them simplifies the persistence model and the DownloadManager -> DownloadWorker communication.

### 6. DownloadManager no longer resolves routes

**Decision:** DownloadManager drops its `IRouteResolver` dependency. `InitDownload` command no longer carries `routeName`/`proxyUrl`. The worker just passes the channel through to the Remuxer.

**Why:** The manager was resolving routes only to pass them through to the worker who passed them through to the Remuxer. Three hops for something the Remuxer can do in one.

## Risks / Trade-offs

- **Route config changes mid-download** - With the old design, the route was frozen at enqueue time. Now it's resolved at download time. This means a config change takes effect on the next download attempt, which is the expected behavior.
- **v0.x persistence break** - Existing persisted DownloadInitialized events with RouteName/ProxyUrl will fail to deserialize. Mitigation: clear the SQLite journal or accept the break at v0.x.
- **More internal interfaces** - Two new internal interfaces (ISubtitleDownloader, IFfmpegProcess). Mitigation: they're internal, invisible to consumers, and each has exactly one implementation.
