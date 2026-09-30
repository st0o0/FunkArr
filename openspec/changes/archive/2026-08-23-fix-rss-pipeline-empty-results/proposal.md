## Why

The RSS feed endpoint (`t=tvsearch` with no parameters) returns 0 results despite the MediathekViewWeb API returning 100 items. The root cause is a JSON deserialization mismatch: `MediathekClient` uses `JsonNamingPolicy.CamelCase` which transforms `Url_Video_HD` to `url_Video_HD`, but the API returns `url_video_hd` (lowercase snake_case). With case-sensitive matching (the default), all URL fields silently stay as `string.Empty`, causing `ExpandWithProbingAsync` to produce zero `SearchResult` variants per item. A secondary issue is that MediathekViewWeb returns protocol-relative URLs (`//host/path`) which fail HTTP probing and downstream processing.

## What Changes

- Fix JSON deserialization of `MediathekResultItem` URL fields by adding explicit `[JsonPropertyName]` attributes matching the MediathekViewWeb API contract (`url_video`, `url_video_hd`, `url_video_low`, `url_subtitle`, `url_website`)
- Normalize protocol-relative URLs (`//`) to `https://` when constructing `SearchResult` entries, so probing and downstream consumers receive valid absolute URIs
- Fix HD quality tier inconsistency: `ExpandWithProbingAsync` uses `HD720` as fallback for `Url_Video_HD`, while the sync path `ExpandQualities` correctly maps it to `HD1080`

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `mediathek-search`: URL fields must be annotated with explicit JSON property names to match the API contract; URL normalization must be applied before results leave the search pipeline
- `quality-probing`: `ExpandWithProbingAsync` must use `HD1080` as the fallback tier for `Url_Video_HD`, matching the sync path behavior

## Impact

- `src/FunkArr/Search/MediathekClient.cs` — `[JsonPropertyName]` attributes on `MediathekResultItem`
- `src/FunkArr/Search/QualityProbeService.cs` — HD fallback tier fix, URL normalization
- `src/FunkArr/Search/MatchingPipeline.cs` — URL normalization in `CreateResult`
- Existing tests for `MatchingPipeline` and `QualityProbeService` may need URL field updates
