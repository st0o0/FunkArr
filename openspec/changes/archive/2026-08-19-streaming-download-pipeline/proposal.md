## Why

The download execution path currently uses one DownloadWorkerActor per download and a single sequential MuxingActor, making it architecturally unable to scale beyond a handful of concurrent downloads. At 20 concurrent downloads, muxing becomes a bottleneck (sequential processing), worker actor overhead grows unnecessarily, and there is no backpressure between stages. The matching pipeline exists as disconnected utility methods rather than a composed flow, and the Newznab/SABnzbd endpoints are still stubbed. This change replaces the worker-per-download model with an Akka.Streams pipeline inside the DownloadQueueActor (proven pattern from the Njord reference project), composes the matching pipeline as pure LINQ functions, and wires the API endpoints to complete the end-to-end flow.

## What Changes

- **Replace DownloadWorkerActor + MuxingActor with Akka.Streams pipeline**: `Source.Queue` → `SelectAsyncUnordered(N, download)` → `SelectAsyncUnordered(M, mux)` → `Sink.ForEach(Self.Tell)` materialized inside DownloadQueueActor. Typed outcomes (Success/Failure) flow through the stream — no silent element drops.
- **Configurable per-stage parallelism**: Download concurrency (default 20) and mux concurrency (default 4), both configurable via `FunkArrOptions`.
- **Stream lifecycle management**: `SharedKillSwitch` for cancellation, `StreamSupervision` decider for transient error resilience, `Become + IWithStash` for actor state transitions during materialization/recovery.
- **Compose matching pipeline as pure functions**: `MatchingPipeline.Execute()` as LINQ chain with `MatchContext` carrying show name, season, episode, air date, expected duration. TVDB lookup once per request in SearchActor before pipeline runs.
- **Wire Newznab endpoints to SearchActor**: Complete the stubbed `tvsearch`, `movie`, and `search` handlers.
- **Wire SABnzbd endpoints to DownloadQueueActor**: Complete the stubbed `addfile`, `queue`, and `history` handlers.
- **Remove DownloadWorkerActor**: Logic absorbed into stream download stage.
- **Remove MuxingActor as actor**: FFmpeg/subtitle logic extracted to a service class (`MuxingService`), called from stream mux stage.
- **BREAKING**: `ConcurrentDownloads` default changes from 3 to 20. `MuxConcurrency` added (default 4).

## Capabilities

### New Capabilities
- `stream-supervision`: StreamSupervision decider and stream lifecycle management patterns (KillSwitch, materialization states, typed outcomes).

### Modified Capabilities
- `download-pipeline`: Worker-per-download model replaced by Akka.Streams pipeline with configurable per-stage parallelism and backpressure. DownloadWorkerActor removed.
- `muxing-pipeline`: MuxingActor removed as standalone actor. FFmpeg logic extracted to MuxingService, invoked as stream stage with configurable parallelism.
- `mediathek-search`: Matching pipeline composed as LINQ chain with MatchContext. TVDB lookup wired for show name resolution. Episode matching expanded beyond S##E## patterns.

## Impact

- **Actors removed**: `DownloadWorkerActor`, `MuxingActor` (actor form only — logic preserved)
- **Actors modified**: `DownloadQueueActor` (major rewrite — streams, stash, become states)
- **New files**: `StreamSupervision.cs`, `MuxingService.cs`, `MatchContext.cs`, `DownloadOutcome.cs`/`MuxOutcome.cs`
- **Modified files**: `SearchActor.cs`, `MatchingPipeline.cs`, `FunkArrOptions.cs`, `FunkArrActorSystemSetup.cs`, `NewznabEndpoints.cs`, `SabnzbdEndpoints.cs`
- **Persistence**: New events possible (stream-related lifecycle). Existing DTOs remain compatible — extend-only.
- **Configuration**: New options `MuxConcurrency`, changed default for `ConcurrentDownloads`.
- **Dependencies**: No new packages — `Akka.Streams` already referenced.
