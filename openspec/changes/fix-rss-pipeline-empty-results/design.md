## Context

The RSS feed endpoint returns 0 results because `MediathekClient` deserializes URL fields as empty strings. The `JsonSerializerOptions` uses `PropertyNamingPolicy.CamelCase`, which transforms C# property names like `Url_Video_HD` to `url_Video_HD`. The MediathekViewWeb API returns lowercase snake_case keys (`url_video_hd`). Since `PropertyNameCaseInsensitive` defaults to `false`, the URL fields never match and stay as `string.Empty`. The rest of the pipeline works correctly — all match filters pass for empty MatchContext, but `ExpandWithProbingAsync` skips all empty URLs, producing zero `SearchResult` entries.

A secondary issue: MediathekViewWeb returns protocol-relative URLs (`//host/path`). These cause `HttpClient` to throw `InvalidOperationException` during probing (no base address). The exceptions are caught and fall back to estimation, but downstream consumers (download pipeline, Sonarr/Radarr) also cannot process relative URIs.

## Goals / Non-Goals

**Goals:**
- Fix JSON deserialization so all URL fields from MediathekViewWeb are correctly mapped
- Normalize protocol-relative URLs to absolute `https://` URLs at the pipeline boundary
- Fix HD fallback tier inconsistency between sync and async quality expansion paths
- Preserve all existing targeted search behavior (MatchesShow, MatchesEpisode, IsDurationAcceptable)

**Non-Goals:**
- Changing the CamelCase naming policy for outgoing query serialization (it works correctly for the POST body)
- Adding retry logic or circuit-breaking for API failures (separate concern)
- Changing the 55-minute cache TTL in SearchActor (separate concern)
- Refactoring the dual sync/async expansion paths into one

## Decisions

### Decision 1: `[JsonPropertyName]` attributes over `PropertyNameCaseInsensitive`

Add explicit `[JsonPropertyName("url_video")]` etc. to each URL field on `MediathekResultItem`.

**Alternative considered:** Setting `PropertyNameCaseInsensitive = true` on `JsonSerializerOptions`. This is simpler but applies globally to all fields, risks unintended matches, and obscures the API contract. Explicit attributes document exactly what the MediathekViewWeb API returns and fail loudly if the contract changes.

### Decision 2: Normalize URLs in a shared helper, applied at `SearchResult` construction

Add a static `NormalizeUrl` method that prepends `https:` to protocol-relative URLs (`//`). Apply it in `MatchingPipeline.CreateResult` and `QualityProbeService.ExpandWithProbingAsync` — the two places where raw MediathekViewWeb URLs enter `SearchResult` objects.

**Alternative considered:** Normalizing in `MediathekClient` right after deserialization. This is cleaner (fix once at the source), but `MediathekResultItem` is a DTO that mirrors the API response — mutating it blurs the boundary between external data and internal representation. Normalizing at the `SearchResult` construction boundary keeps the DTO faithful and makes the transformation explicit.

### Decision 3: Fix HD fallback tier in `ExpandWithProbingAsync`

Change `QualityProbeService.cs` line 66 from `(item.Url_Video_HD, QualityTier.HD720)` to `(item.Url_Video_HD, QualityTier.HD1080)`. The sync path `ExpandQualities` already maps HD to `HD1080`. The async path should match.

## Risks / Trade-offs

- **[Risk] API contract drift** — If MediathekViewWeb changes its JSON field names, deserialization silently fails again. → Mitigation: `[JsonPropertyName]` attributes make the expected names explicit. Add a test that verifies deserialization of a sample API response.
- **[Risk] Hardcoding `https://` for protocol-relative URLs** — Some CDNs might serve different content over HTTP vs HTTPS. → Mitigation: All major German public broadcaster CDNs support HTTPS. Protocol-relative URLs are a legacy pattern; `https://` is the correct modern default.
- **[Risk] HD tier change affects quality sorting** — Changing HD fallback from `HD720` to `HD1080` changes result ordering for RSS feeds. → Mitigation: This aligns the async path with the sync path's existing behavior. The change is correct — `Url_Video_HD` is the HD variant and should be treated as such.
