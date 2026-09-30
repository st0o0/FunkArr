## 1. Project Scaffolding

- [x] 1.1 Create solution structure: `src/FunkArr.slnx`, `src/FunkArr/FunkArr.csproj` (Web SDK, net10.0), `src/FunkArr.Tests/FunkArr.Tests.csproj`, `src/FunkArr.Tests.Shared/FunkArr.Tests.Shared.csproj`
- [x] 1.2 Create `src/Directory.Build.props` (net10.0, nullable, implicit usings, version from release-please) and `src/global.json` (SDK pin with rollForward: latestFeature)
- [x] 1.3 Create `src/Directory.Packages.props` with central package management: Akka.Hosting, Akka.Persistence.Sql.Hosting, Akka.Streams, Akka.Logger.Serilog, Servus, Servus.Akka, Serilog stack, Microsoft.Data.Sqlite, xUnit v3, Akka.Hosting.TestKit
- [x] 1.4 Create `Program.cs` with Servus AppBuilder wiring: `FunkArrServiceSetup`, `FunkArrActorSystemSetup`, `FunkArrApplicationSetup`
- [x] 1.5 Create configuration model (`FunkArrOptions`) with ApiKey, DownloadPath, TempPath, ConcurrentDownloads, CacheDuration, PathMapping and bind from appsettings/env vars
- [x] 1.6 Create `appsettings.json` and `appsettings.Development.json` with sensible defaults
- [x] 1.7 Set up Serilog with console sink, environment/thread enrichers, and Akka.Logger.Serilog integration
- [x] 1.8 Add health check endpoints (`/healthz`, `/alive`) including FFmpeg availability check

## 2. Newznab Indexer API

- [x] 2.1 Create `NewznabEndpoints` with route group `/api` and API key validation middleware
- [x] 2.2 Implement `t=caps` endpoint returning Newznab capabilities XML
- [x] 2.3 Implement `t=tvsearch` endpoint (tvdbid, season, ep, q parameters) delegating to SearchActor
- [x] 2.4 Implement `t=movie` endpoint (imdbid, q parameters) delegating to SearchActor
- [x] 2.5 Implement `t=search` endpoint (free-text q parameter) delegating to SearchActor
- [x] 2.6 Implement fake NZB generation endpoint at `/api/fake_nzb` (base64 URL/title encoding in NZB XML)
- [x] 2.7 Create Newznab RSS/XML response builder with quality tier splitting (1080p/720p/480p) and release title formatting (`SHOW.S##E##.GERMAN.QUALITY.WEB.h264-FA`)

## 3. MediathekViewWeb Search

- [x] 3.1 Create `MediathekClient` (typed HttpClient) for POST requests to MediathekViewWeb query API
- [x] 3.2 Create `SearchActor` with in-memory result cache (configurable TTL, default 55min) and rate limiting for outgoing requests
- [x] 3.3 Implement matching pipeline: title normalization (umlauts, special chars), runtime duration filter (35% threshold), skip keyword filter, S##E## pattern extraction
- [x] 3.4 Create TVDB client for show metadata lookup (resolve tvdbid → German show title, episode titles, air dates)
- [x] 3.5 Implement date matching (compare aired date against dates in title/description)

## 4. SABnzbd Download Client API

- [x] 4.1 Create `SabnzbdEndpoints` with route group `/download/api` and API key validation
- [x] 4.2 Implement `mode=version` endpoint
- [x] 4.3 Implement `mode=get_config` endpoint
- [x] 4.4 Implement `mode=addfile` endpoint (parse fake NZB, extract URL, send to DownloadQueueActor)
- [x] 4.5 Implement `mode=queue` endpoint (query DownloadQueueActor for active jobs with progress)
- [x] 4.6 Implement `mode=history` endpoint (query DownloadQueueActor for completed/failed jobs)
- [x] 4.7 Implement download path mapping in history/queue responses

## 5. Download Pipeline

- [x] 5.1 Create `DownloadQueueActor` as persistent actor (Akka.Persistence) with BackoffSupervisor, managing job queue state (Queued, Downloading, Muxing, Completed, Failed)
- [x] 5.2 Define persistence events: DownloadEnqueued, DownloadStarted, DownloadProgressUpdated, DownloadCompleted, DownloadFailed, MuxingStarted, MuxingCompleted, MuxingFailed
- [x] 5.3 Create `DownloadWorkerActor` (transient, one per active download) with HTTP chunked download and progress reporting
- [x] 5.4 Implement OneForOneStrategy supervision in DownloadQueueActor: restart workers on transient failures (max 3 retries), stop on permanent failures
- [x] 5.5 Implement concurrency control: DownloadQueueActor tracks active worker count, starts new workers when slots open
- [x] 5.6 Configure Akka.Persistence.Sql with SQLite backend (journal + snapshot store)
- [x] 5.7 Implement queue recovery on startup (replay events, re-queue in-progress downloads)

## 6. Muxing Pipeline

- [x] 6.1 Create `MuxingActor` (singleton, stateless) that receives completed downloads and runs FFmpeg
- [x] 6.2 Implement FFmpeg process wrapper: build command line, start process, capture stdout/stderr, enforce timeout (default 10min)
- [x] 6.3 Implement subtitle format detection and VTT/TTML → SRT conversion
- [x] 6.4 Implement temp file cleanup (delete source files on success, preserve on failure)
- [x] 6.5 Wire MuxingActor into download flow: DownloadWorkerActor completes → DownloadQueueActor sends to MuxingActor → result updates queue state

## 7. Docker & CI/CD

- [x] 7.1 Create `Dockerfile` (two-stage: prep + runtime, based on aspnet:10.0-noble-chiseled-extra, FFmpeg static binary, ports 8080, volumes /app/data and /media)
- [x] 7.2 Create `docker-compose.example.yml` with reference configuration (env vars, volume mounts, port mapping)
- [x] 7.3 Create GitHub Actions CI workflow (`ci.yml`): build, test, dotnet format check, hadolint on PR
- [x] 7.4 Create GitHub Actions release workflow (`release.yml`): release-please versioning, dotnet publish for linux-x64 and linux-arm64, multi-arch Docker build, push to GHCR
- [x] 7.5 Create `.env.example` documenting all configuration options

## 8. Testing

- [x] 8.1 Unit tests for Newznab XML/RSS response builder and fake NZB generation
- [x] 8.2 Unit tests for matching pipeline (title normalization, duration filter, skip keywords, S##E## patterns)
- [x] 8.3 Actor tests for SearchActor (caching, rate limiting) using Akka.Hosting.TestKit
- [x] 8.4 Actor tests for DownloadQueueActor (enqueue, concurrency, persistence recovery) using Akka.Persistence.TestKit
- [x] 8.5 Integration tests for Newznab and SABnzbd endpoints using WebApplicationFactory
