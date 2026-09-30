## Context

FunkArr uses Servus AppBuilder with SetupContainers for DI, actor registration, and endpoint mapping. The current split mixes domain concerns: ServiceSetupContainer holds registrations from RuleSet, Download, and Core; actor registrations are centralized in AkkaSetupContainer regardless of domain; options from Scoring/History leak into RuleSetSetupContainer.

The download remux layer has three classes (Remuxer, FfmpegRunner, SubtitlePreparer) with three interfaces where a single orchestrator would suffice. The FFmpeg argument builder lacks stream mapping and HLS awareness, causing SRF/ORF downloads to fail.

## Goals / Non-Goals

**Goals:**
- Each domain owns a single SetupContainer with its DI, actors, and endpoints
- Clear principle: "if it belongs to domain X, it's in XSetupContainer"
- AkkaSetupContainer only configures the actor system infrastructure
- Remuxer becomes the single download orchestrator (subtitle prep + FFmpeg execution)
- Clean internal API via RemuxOptions record
- SRF/ORF HLS downloads work (stream mapping + BSF tolerance)

**Non-Goals:**
- No changes to domain project structure (Search, Download, etc. stay separate projects)
- No Servus framework changes (AppBuilder, ISetupContainer interfaces stay)
- No yt-dlp integration
- No changes to DownloadWorker, subtitle format classes, or message types
- No API or persistence changes

## Decisions

### 1. Per-domain SetupContainers own their actors

**Decision:** Move actor registrations from AkkaSetupContainer into their domain containers. Each domain container calls `WithResolvableActors` and `WithShardRegion` for its own actors.

**Why:** The current AkkaSetupContainer registers 11 singletons and 5 shard regions from 6 different domains. Adding an actor means editing a file that has nothing to do with your domain. Per-domain containers make the dependency between "I registered this service" and "I registered this actor that uses it" visible in one place.

**Alternative:** Keep actors centralized but group them by domain in sections. Rejected because it still means touching a shared file and the grouping would drift.

**How Servus supports this:** `ActorSystemSetupContainer` calls `WithResolvableActors` which is additive. Multiple containers can each call it. The final actor system gets all registrations. Same for `WithShardRegion`. The key is that `AkkaSetupContainer` still creates the ActorSystem and configures persistence/clustering - domain containers just add their actors to it.

### 2. ServiceSetupContainer splits into CoreSetupContainer + domain containers

**Decision:** Create CoreSetupContainer for cross-cutting infrastructure (FunkArrOptions, DataPaths, RoutingOptions, IRouteResolver, IFileSystem, JSON config). Domain-specific registrations move to their domain containers. ServiceSetupContainer is deleted.

**Why:** ServiceSetupContainer currently has no clear scope. RuleSetStore, ArrApiClient, health checks from multiple domains, and core infrastructure all coexist. The name "Service" gives no guidance on what belongs.

### 3. New ScoringSetupContainer and ArrApiSetupContainer

**Decision:** Create two new containers: ScoringSetupContainer (owns ScoringOptions, ScoringHistoryOptions, Scoring/History actors) and ArrApiSetupContainer (owns Newznab/SABnzbd services, ApiKeyFilter, controller mapping).

**Why:** ScoringOptions currently leaks into RuleSetSetupContainer. ArrApi services currently leak into DownloadSetupContainer. Both are distinct concerns that deserve their own container.

### 4. Remuxer absorbs FfmpegRunner and SubtitlePreparer

**Decision:** Merge all three into Remuxer. Remove IFfmpegRunner and ISubtitlePreparer interfaces. Keep IRemuxer as the only public interface.

**Why:** FfmpegRunner has exactly one consumer (Remuxer). SubtitlePreparer has exactly one consumer (Remuxer). The three-class split creates interface ceremony without testability benefit - the static methods (BuildArguments, ParseProgressLine, ClassifyFailure, ExtractError) are already testable without mocking. The subtitle download logic needs IHttpClientFactory which Remuxer can take via DI.

**Testing:** BuildArguments remains `internal static` and fully unit-testable. ParseProgressLine, ClassifyFailure, ExtractError remain `internal static`. Subtitle format parsing is tested through the format classes. The only thing that loses separate-unit-test coverage is the orchestration glue in Remuxer.RunAsync, which is thin enough to test via integration or the existing DownloadWorker tests.

### 5. RemuxOptions as internal record, not a public fluent builder

**Decision:** RemuxOptions is a simple `internal sealed record` constructed by Remuxer, not a public fluent builder API.

```
internal sealed record RemuxOptions(
    string VideoUrl, string? SubtitlePath, string OutputPath,
    string? ProxyUrl, string? SubtitleLanguage)
{
    internal bool IsHls => VideoUrl.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase);
}
```

**Why:** The only consumer is Remuxer itself. A fluent builder is overkill for an internal record that nobody constructs from outside. The record is clear, immutable, and self-documenting. `IsHls` is a computed property, not stored.

### 6. Always map video + audio, BSF tolerance for HLS only

**Decision:** Apply `-map 0:v:0 -map 0:a:0` for all inputs. Apply `-bsf:a aac_adtstoasc=no_validation=1` only when `IsHls` is true.

**Why:** Explicit mapping is safe for both MP4 and HLS and filters data streams (timed_id3, timecode). BSF tolerance is only needed for HLS because direct MP4 has no ADTS headers. Verified by testing with SRF Tagesschau (HLS) and ARD Tagesschau (direct MP4).

### 7. Container ordering in Program.cs

**Decision:** The AppBuilder chain order becomes:
1. LoggingSetupContainer (must be first)
2. TelemetrySetupContainer
3. CoreSetupContainer
4. AkkaSetupContainer (actor system infra)
5. SearchSetupContainer (domain + actors)
6. DownloadSetupContainer (domain + actors)
7. ScoringSetupContainer (domain + actors)
8. RuleSetSetupContainer (domain + actors)
9. EnrichmentSetupContainer (domain + actors)
10. ArrApiSetupContainer (adapter + controllers)
11. ApplicationSetupContainer (middleware, endpoints, must be last)

**Why:** Infrastructure first, then domains (order doesn't matter between them), then adapters, then app pipeline.

## Risks / Trade-offs

- **Actor registration across multiple containers** - If Servus's `WithResolvableActors` doesn't compose across multiple containers, we need a fallback. Mitigation: verify this works in a test before committing to the pattern. If it doesn't, domain containers can return their registrations and AkkaSetupContainer applies them.
- **Remuxer class size** - Absorbing FfmpegRunner and SubtitlePreparer makes Remuxer larger (~250 lines). Mitigation: the static helper methods can move to a `RemuxHelpers` internal static class if Remuxer grows beyond comfort.
- **Test DI changes** - Tests that mock IFfmpegRunner or ISubtitlePreparer need updating. Mitigation: BuildArguments tests replace FfmpegRunner unit tests. SubtitlePreparer tests stay via the format classes.
