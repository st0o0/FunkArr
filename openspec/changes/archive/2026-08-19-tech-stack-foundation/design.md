## Context

FunkArr is a greenfield rewrite of MediathekArr that bridges German public broadcaster media libraries (ARD, ZDF, etc.) into the *arr ecosystem. The *arr apps (Sonarr, Radarr, Prowlarr) expect two standard interfaces: a Newznab-compatible indexer for search and a SABnzbd-compatible download client for fetching content. MediathekArr proved the concept works but suffers from code duplication, no fault tolerance, and brittle metadata matching.

Reference architecture: Njord (D:\GIT\Njord) — same author, same patterns (Servus AppBuilder, Akka.Hosting, SQLite persistence, multi-arch Docker).

## Goals / Non-Goals

**Goals:**
- Protocol-compatible Newznab indexer that Prowlarr/Sonarr/Radarr can use without modification
- Protocol-compatible SABnzbd download client with queue, history, and progress tracking
- Fault-tolerant download pipeline with supervision and automatic retry
- Download queue that survives application restarts (SQLite persistence)
- Configurable concurrent downloads (default 3)
- FFmpeg-based remuxing of video + subtitles into MKV
- Single Docker image, multi-arch (amd64 + arm64), suitable for homelab deployment

**Non-Goals:**
- Cluster/multi-node deployment — single instance is sufficient for homelab use
- Re-encoding video content — stream-copy only
- Web UI — the *arr apps are the UI
- Prometheus metrics / gRPC — not needed for v0.1.0
- HLS/DASH stream downloading — direct HTTP downloads only for v0.1.0
- Custom matching rulesets (MediathekArr issue #90) — future enhancement

## Decisions

### 1. Single Project, Namespace-Based Layering

**Choice:** One `FunkArr.csproj` (Web SDK) with internal namespaces, not separate class libraries.

**Alternatives considered:**
- Separate `FunkArr.Indexer` + `FunkArr.DownloadClient` projects — rejected because they share the same models, MediathekViewWeb client, and actor system. Splitting adds friction without value at this scale.

**Rationale:** Njord uses the same pattern successfully. The codebase is small enough that namespace separation (Indexer/, DownloadClient/, Shared/) provides sufficient organization without the overhead of multi-project dependency management.

### 2. Akka.NET with Servus Patterns

**Choice:** Akka.Hosting + Servus AppBuilder for actor system integration.

```
FunkArrServiceSetup       → DI: HttpClient, Options, Serilog, HealthChecks
FunkArrActorSystemSetup   → Akka: Actors, Persistence, Supervision
FunkArrApplicationSetup   → HTTP: Minimal API endpoint mapping
```

**Rationale:** Proven pattern from Njord. Akka.NET provides supervision trees that match the download pipeline's needs perfectly — isolate failures per download, restart failed workers, escalate systemic issues (CDN down).

### 3. Actor Hierarchy

```
/user (ActorSystem guardian)
  │
  ├── /search-actor                    (singleton, cache + rate limit)
  │     Restart on failure, cache loss acceptable
  │
  ├── /download-queue                  (persistent, BackoffSupervisor)
  │     │  Manages queue state, dispatches to workers
  │     │  Resume on child failure (preserve queue)
  │     │
  │     ├── /worker-1                  (transient, created per download)
  │     ├── /worker-2                  
  │     └── /worker-N                  (N = configurable concurrency)
  │           OneForOneStrategy: Restart on failure (max 3 retries)
  │           Download HTTP stream → temp file
  │
  └── /muxing-actor                    (singleton, stateless)
        Restart on failure
        Receives completed downloads, runs FFmpeg, moves to output
```

### 4. SQLite Persistence via Akka.Persistence.Sql

**Choice:** Akka.Persistence.Sql.Hosting with SQLite backend for the DownloadQueueActor.

**Alternatives considered:**
- In-memory only — rejected because queue loss on restart means re-downloading completed content
- Raw SQLite without Akka.Persistence — rejected because event-sourced recovery integrates naturally with actor lifecycle
- PostgreSQL — rejected, unnecessary for single-instance homelab deployment

**What gets persisted:**
- Download queue entries (URL, target path, status, progress, retry count)
- Completed download history (for SABnzbd history endpoint)

**What stays in-memory:**
- Search cache (SearchActor) — ephemeral, rebuilds on demand
- Active download progress (WorkerActors) — transient by nature

### 5. Minimal API Endpoints

**Choice:** Minimal API with endpoint groups, no controllers.

```
/api              → NewznabEndpoints    (t=caps, t=tvsearch, t=search, t=movie)
/api/fake_nzb     → NzbEndpoints        (fake NZB download)
/download/api     → SabnzbdEndpoints    (mode=version/queue/history/addfile)
/healthz          → HealthCheckEndpoints
```

**Rationale:** Two API surfaces with ~5 endpoints each. Controllers would add ceremony without benefit. Endpoint groups keep related routes together.

### 6. FFmpeg for Muxing

**Choice:** FFmpeg with stream-copy (no re-encoding).

```
ffmpeg -i video.mp4 -i subtitles.srt \
  -map 0:v -map 0:a -map 1:s \
  -c copy -c:s srt \
  -metadata:s:v:0 language=ger \
  -metadata:s:a:0 language=ger \
  -metadata:s:s:0 language=ger \
  output.mkv
```

**FFmpeg binary strategy:** Bundled in Docker image as static binary. For local development, expect FFmpeg on PATH.

### 7. The Fake NZB Trick

Carried over from MediathekArr — this is the core insight that makes the integration work:

1. Search returns Newznab RSS with a download link pointing to `/api/fake_nzb?url=<base64>&title=<base64>`
2. Prowlarr/Sonarr fetches this link, receives a valid-looking NZB XML file
3. The NZB contains the real HTTP download URL encoded in XML comments
4. When Sonarr sends the NZB to the SABnzbd API (`mode=addfile`), FunkArr extracts the real URL and starts the actual download

### 8. Docker Image

```
Base:    mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra
FFmpeg:  Static binary copied from build stage or fetched in CI
Ports:   8080 (HTTP)
Volumes: /app/data (SQLite DB + download temp)
         /media (completed downloads — mapped to Sonarr/Radarr media path)
```

CI cross-compiles for linux-x64 and linux-arm64, no SDK in the final image.

## Risks / Trade-offs

- **MediathekViewWeb API stability** → No official API contract; the query endpoint could change without notice. Mitigation: isolate the client behind an interface, add integration tests against the real API.
- **Broadcaster CDN rate limiting / 403s** → MediathekArr issue #83. Mitigation: configurable rate limiting in SearchActor and per-download retry with exponential backoff in WorkerActors.
- **Metadata matching quality** → Biggest pain point from MediathekArr. German broadcasters have inconsistent naming. Mitigation: port existing matching logic first, improve iteratively. This is a content problem, not an architecture problem.
- **FFmpeg binary availability** → Must be present in Docker image and on dev machines. Mitigation: health check that verifies FFmpeg presence at startup.
- **SQLite under concurrent writes** → WAL mode handles the single-writer pattern of Akka.Persistence well. Not a real risk at this scale.

## Open Questions

- Should FunkArr auto-download FFmpeg on first run (like MediathekArr does) or require it pre-installed? Recommendation: require it — simpler, and Docker image always has it.
- Subtitle format handling — broadcasters provide SRT, VTT, or TTML. Do we normalize to SRT before muxing or pass through? Recommendation: normalize to SRT for maximum player compatibility.
