## Context

FunkArr currently assigns quality tiers by position: `Url_Video_HD` → 1080p, `Url_Video` → 720p, `Url_Video_Low` → SD. This is identical to what MediathekArr and RundfunkArr (TypeScript) do. The MediathekViewWeb API provides no resolution, codec, or per-URL size data. The codec is hardcoded as "h264" in release titles. File sizes are either the single API-provided value (for one URL only) or estimated from fixed bitrate assumptions.

The quality expansion logic is duplicated between `MatchingPipeline.cs` and `SearchActor.cs` — this change should also consolidate it.

## Goals / Non-Goals

**Goals:**
- Detect real resolution, codec, and file size for each video URL via lightweight probing
- Replace hardcoded quality tier assignments with verified data
- Emit rich Newznab attributes so Sonarr/Prowlarr can make correct quality decisions
- Cache probe results to avoid redundant network requests
- Consolidate duplicated quality expansion logic into `QualityProbeService`

**Non-Goals:**
- Full video stream analysis (no FFprobe, no full download)
- HLS/DASH stream probing (m3u8/mpd URLs are passed through with best-guess quality)
- Audio quality detection beyond codec identification
- Changing the download pipeline or muxing system

## Decisions

### 1. Three-phase probing with escalation

**Decision:** Probe quality in three phases, each more expensive than the last. Stop as soon as sufficient data is obtained.

**Phase 1 — URL Pattern Analysis (zero cost):**
German broadcaster CDN URLs contain quality hints:
- ZDF: `2256k_p18v17.mp4` → bitrate 2256kbps, profile p18 (720p h264)
- ZDF: `6660k_p37v17.mp4` → bitrate 6660kbps, profile p37 (1080p h265)
- ARD: `/720/master.m3u8` → 720p
- arte: `1080` in path → 1080p

A `UrlPatternAnalyzer` with broadcaster-specific regex patterns extracts resolution and bitrate from the URL string alone.

**Phase 2 — HTTP HEAD (∼200 bytes per URL):**
`Content-Length` gives the real file size. `Content-Type` reveals the container format (`video/mp4`, `video/webm`, `application/x-mpegURL`). This replaces the guessed/estimated size.

**Phase 3 — Container Header Probe (∼32KB, optional):**
For direct-download URLs (not HLS/DASH) where Phase 1 could not determine resolution or codec: download the first 32KB via `Range: bytes=0-32767` and parse the MP4 `ftyp`+`moov` atoms to extract video track width/height, codec FourCC, and bitrate. This gives definitive quality data.

**Rationale:** Phase 1 alone resolves most URLs (ZDF URLs are highly structured). Phase 2 is almost free and always useful for size. Phase 3 is the fallback for ambiguous URLs. This minimizes network cost while maximizing accuracy.

**Alternative considered:** Using FFprobe for full analysis. Rejected — it requires downloading significant data, adds a process spawn per URL, and is overkill when container headers are sufficient.

### 2. QualityProbeService as a DI service, not an actor

**Decision:** Implement `QualityProbeService` as a regular DI-registered service (not an Akka.NET actor), injected into SearchActor.

**Rationale:** Probing is a stateless I/O operation — it sends HTTP requests and parses responses. It doesn't need actor supervision, message-based concurrency, or persistence. A DI service with `IHttpClientFactory` is simpler and more testable. The SearchActor already handles concurrency via its mailbox.

**Alternative considered:** A `QualityProbeActor` with ask-based queries. Rejected — adds unnecessary message ceremony for what is essentially an HTTP client wrapper.

### 3. Quality cache as ConcurrentDictionary with LazyValueCache

**Decision:** Cache probe results in a `ConcurrentDictionary<string, QualityInfo>` keyed by URL, with TTL-based expiry via Servus.Core's `LazyValueCache` pattern. Default TTL: 6 hours (Mediathek URLs are stable for days/weeks).

**Rationale:** Mediathek video URLs don't change — the same URL always returns the same file. A long cache TTL is safe. In-memory is sufficient since quality data can be re-probed cheaply after restart. ConcurrentDictionary provides thread-safe access from concurrent SearchActor invocations.

**Alternative considered:** A separate `QualityCacheActor`. Rejected — same reasoning as Decision 2, this is simple key-value storage.

### 4. Probing at search time, not download time

**Decision:** Probe quality during search result processing (in the SearchActor), before returning Newznab results to Sonarr/Prowlarr.

**Rationale:** Sonarr makes grab decisions based on the quality reported in search results. If quality is only verified at download time, Sonarr has already committed to a potentially wrong quality. Search-time probing means Sonarr sees correct quality from the start.

**Trade-off:** Search latency increases by the probe time. Mitigated by: (1) URL pattern analysis is instant, (2) HEAD requests are fast (∼50ms each), (3) results are cached so repeat searches are instant, (4) probing only non-cached URLs.

### 5. Probe only top-scored results, not all 5000

**Decision:** After the matching pipeline produces scored results, probe quality for the top N results (default: 30) rather than all matched results.

**Rationale:** MediathekViewWeb can return thousands of results, but after matching/filtering typically 5-20 remain per quality tier. Probing 30 URLs means at most 30 HEAD requests (cached after first probe). This keeps latency bounded while covering all results Sonarr will actually consider.

### 6. QualityInfo as a value record

**Decision:** Define `QualityInfo` as an immutable record containing: `Resolution` (width × height), `QualityTier` (derived from resolution), `Codec` (string: "h264", "h265", "vp9", "av1"), `Bitrate` (kbps, nullable), `FileSize` (bytes), `Container` (string: "mp4", "mkv", "webm"), `ProbeSource` (enum: UrlPattern, Head, ContainerHeader, Estimated).

**Rationale:** `ProbeSource` tracks how the quality was determined. This enables Match Intelligence integration — the Match Ledger can show whether quality was verified or estimated. `QualityTier` is derived from resolution height: ≥1080 → HD1080, ≥720 → HD720, else SD.

### 7. MP4 atom parser — minimal, hand-rolled

**Decision:** Implement a minimal MP4 atom parser that reads `ftyp` and traverses `moov` → `trak` → `mdia` → `minf` → `stbl` → `stsd` to find the video codec FourCC and resolution. No external library.

**Rationale:** The MP4 atom structure is well-documented and the video sample description is at a fixed position in the atom hierarchy. A 50-line parser is sufficient — we only need the codec and dimensions from the first video track. Adding a media parsing library (like TagLibSharp or MediaToolkit) for this single use case is overkill.

**Limitation:** WebM/MKV uses EBML, not atoms. Phase 3 container parsing only works for MP4. For WebM, fall back to Phase 1 + Phase 2 data.

## Risks / Trade-offs

- **Search latency increase from HEAD requests** → Mitigated by caching (6h TTL) and limiting probing to top 30 results. First search for a show is slower; subsequent searches use cache.
- **Broadcaster CDN may reject HEAD/Range requests** → Mitigated by graceful fallback: if HEAD fails, use estimated size; if Range fails, skip Phase 3 and use Phase 1 + Phase 2 data. Never block a search result because probing failed.
- **URL pattern regexes become stale if CDNs restructure** → Mitigated by Phase 2/3 fallback. URL patterns are a fast path, not a requirement.
- **MP4 moov atom not in first 32KB** → Some encoders place `moov` at the end of the file. For these, Phase 3 fails gracefully and falls back to Phase 1 + 2 data. This is rare for streaming-optimized files (which use `faststart`/`moov` before `mdat`).

## Open Questions

- Should probe failures be reported to Match Intelligence (if both changes land)? Leaning yes — a "quality unknown" entry in the ledger is useful for debugging.
- Should there be a config option to disable probing entirely (for users who prefer fast searches over accurate quality)? Probably yes as a simple boolean toggle.
