## Context

The download system has two recently added features: speed limiting and time-based scheduling. Speed limiting is broken — it uses `-maxrate`/`-bufsize` which are encoder flags ignored during codec-copy remux. Scheduling works but uses `DateTime.Now`, making it untestable. Both specs have placeholder Purpose sections.

## Goals / Non-Goals

**Goals:**
- Remove all speed limiting code and configuration cleanly
- Make download scheduling testable via `TimeProvider` injection
- Fix incomplete spec Purpose sections

**Non-Goals:**
- Re-implementing speed limiting with a different approach (can revisit later)
- Changing scheduling behavior (only making it testable)
- Adding runtime settings mutation via API (settings remain config-file-only)

## Decisions

### Remove speed limit rather than fix it

**Choice**: Delete the feature entirely.

**Alternatives considered**:
- Fix with `-readrate` + ffprobe: adds complexity (extra HTTP request per download, bitrate-to-factor calculation, ffprobe dependency). Mediathek servers already rate-limit themselves. Not worth it.
- Fix with `-readrate` heuristic: inaccurate without knowing stream bitrate. Would create a false sense of control.

**Rationale**: The feature was silently broken since day one. No user has reported issues from lack of throttling. Clean removal is simpler than a fix that adds marginal value.

### TimeProvider via constructor injection

**Choice**: Add `TimeProvider` as a constructor parameter to `DownloadManager`, resolved via `resolver.Props<T>()`.

**Alternatives considered**:
- Static `TimeProvider.System` with test override: harder to test, global state.
- Pass `TimeOnly now` as parameter to `DispatchNext`: would require changing internal message handling flow.

**Rationale**: Constructor injection follows the existing pattern in `SearchResultCache` and is the standard .NET 8+ approach. `TimeProvider.System` is the default registration — no explicit DI setup needed since it's already in the framework DI container.

### Speed limit parameter removal is interface-breaking

**Choice**: Remove `speedLimitBytesPerSecond` parameter from `IFfmpegRunner.RunAsync()`, `IRemuxer.RunAsync()`, and all call sites in one step.

**Rationale**: Version 0.x — breaking changes are fine. No external consumers of these interfaces. Clean removal is better than leaving dead parameters.

## Risks / Trade-offs

- [Risk] Users who configured `SpeedLimitBytesPerSecond` in their config will get an unknown-key warning on startup → Mitigation: This is standard .NET config behavior, no action needed. The config key is silently ignored.
- [Risk] Future need for speed limiting requires re-implementation from scratch → Mitigation: Acceptable. If needed, a proper implementation (e.g., throttled HTTP client wrapper or `-readrate` with ffprobe) would be better than patching the broken approach.
