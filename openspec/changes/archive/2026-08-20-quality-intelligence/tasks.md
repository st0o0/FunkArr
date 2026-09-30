## 1. QualityInfo Model

- [x] 1.1 Define `QualityInfo` record: Resolution (width/height), QualityTier (derived), Codec (string), Bitrate (int?, kbps), FileSize (long, bytes), Container (string), ProbeSource (enum: UrlPattern, Head, ContainerHeader, Estimated)
- [x] 1.2 Add `ProbeSource` enum and `QualityTier` derivation logic (height >= 1080 → HD1080, >= 720 → HD720, else SD)
- [x] 1.3 Extend `SearchResult` model with `QualityInfo` field replacing separate QualityTier/SizeBytes
- [x] 1.4 Unit tests for QualityTier derivation from resolution

## 2. URL Pattern Analyzer (Phase 1)

- [x] 2.1 Create `UrlPatternAnalyzer` static class with broadcaster-specific regex patterns
- [x] 2.2 Implement ZDF pattern: extract bitrate and profile code from URL (e.g., `2256k_p18v17`) with profile-to-resolution mapping
- [x] 2.3 Implement ARD pattern: extract resolution from path segments (e.g., `/720/`, `/1080/`)
- [x] 2.4 Implement arte pattern: extract resolution hints from URL path
- [x] 2.5 Implement HLS detection (`.m3u8` URLs flagged as non-probeable for Phase 3)
- [x] 2.6 Unit tests for each broadcaster pattern (match, no-match, edge cases)

## 3. HTTP HEAD Probing (Phase 2)

- [x] 3.1 Create `QualityProbeService` class with `IHttpClientFactory` injection
- [x] 3.2 Implement `ProbeHeadAsync`: send HEAD request, extract Content-Length and Content-Type, 5s timeout
- [x] 3.3 Handle HEAD failures gracefully (403, 405, timeout → return null)
- [x] 3.4 Unit tests with mocked HttpMessageHandler for success, failure, and timeout scenarios

## 4. Container Header Probing (Phase 3)

- [x] 4.1 Implement `ProbeContainerAsync`: send Range request for bytes 0-32767, validate 206 response
- [x] 4.2 Implement minimal MP4 atom parser: traverse ftyp → moov → trak → mdia → minf → stbl → stsd to extract video codec FourCC and resolution
- [x] 4.3 Map codec FourCC to string: avc1/avc3 → "h264", hev1/hvc1 → "h265", vp09 → "vp9", av01 → "av1"
- [x] 4.4 Handle non-206 responses (full content returned) — abort and return null
- [x] 4.5 Handle moov-at-end (moov not in first 32KB) — return null gracefully
- [x] 4.6 Unit tests with sample MP4 atom data for codec/resolution extraction

## 5. Probing Orchestration

- [x] 5.1 Implement `ProbeAsync(string url)` orchestrator: Phase 1 → Phase 2 → Phase 3 (conditional), build QualityInfo from combined results
- [x] 5.2 Skip Phase 3 when Phase 1 provides both resolution and codec
- [x] 5.3 Skip Phase 3 for HLS/DASH URLs (m3u8, mpd)
- [x] 5.4 Return estimated QualityInfo when all phases fail (conservative: Url_Video_HD → HD720)
- [x] 5.5 Add `FunkArr__QualityProbing` boolean option (default true) to disable all probing
- [x] 5.6 Integration tests for full probe orchestration with mocked HTTP responses

## 6. Quality Cache

- [x] 6.1 Implement quality cache as `ConcurrentDictionary<string, CacheEntry<QualityInfo>>` with TTL-based expiry
- [x] 6.2 Add `FunkArr__QualityCacheTtlMinutes` option (default 360)
- [x] 6.3 Add capacity limit (default 50,000) with oldest-entry eviction
- [x] 6.4 Integrate cache into `QualityProbeService.ProbeAsync` — check cache before probing, store after
- [x] 6.5 Ensure thread-safe concurrent access (single probe per URL under concurrent requests)
- [x] 6.6 Unit tests for cache hit, miss, expiry, capacity eviction, concurrent access

## 7. Search Integration

- [x] 7.1 Register `QualityProbeService` in `FunkArrServiceSetup` DI container
- [x] 7.2 Consolidate duplicated `ExpandQualities`/`CreateResult`/`EstimateSize` from MatchingPipeline and SearchActor into `QualityProbeService.ExpandWithProbing`
- [x] 7.3 Update `SearchActor` to call `QualityProbeService.ExpandWithProbing` for top N results (configurable, default 30)
- [x] 7.4 Update `MatchingPipeline` to use same consolidated expansion path
- [x] 7.5 Add `FunkArr__QualityProbeLimit` option (default 30) for max probed results per search
- [x] 7.6 Update `FunkArrOptions` and `FunkArrOptionsValidator` with new quality options

## 8. Newznab XML Enrichment

- [x] 8.1 Update `NewznabXmlBuilder` to emit dynamic codec in release title from QualityInfo.Codec instead of hardcoded "h264"
- [x] 8.2 Add `newznab:attr` for `resolution` (e.g., "1080p") — only when ProbeSource != Estimated
- [x] 8.3 Add `newznab:attr` for `video` (codec string)
- [x] 8.4 Add `newznab:attr` for `language` (always "German")
- [x] 8.5 Add `newznab:attr` for `tvdbid`, `season`, `episode` (from search context, when available)
- [x] 8.6 Use real file size from QualityInfo in `<enclosure length="">` and `newznab:attr name="size"`
- [x] 8.7 Update conservative default: Url_Video_HD → 720p (not 1080p) when quality is estimated
- [x] 8.8 Update existing Newznab tests for new attributes and dynamic codec

## 9. Configuration and Documentation

- [x] 9.1 Add all new options to `appsettings.json` with defaults (QualityProbing=true, QualityCacheTtlMinutes=360, QualityProbeLimit=30)
- [x] 9.2 Document new environment variables in `docker-compose.example.yml`
