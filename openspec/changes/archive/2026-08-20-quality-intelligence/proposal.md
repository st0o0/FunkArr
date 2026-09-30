## Why

All three Mediathek-to-arr projects (MediathekArr, RundfunkArr TypeScript, FunkArr) hardcode quality assumptions: `Url_Video_HD` = 1080p, `Url_Video` = 720p, codec = h264. In reality, ARD often serves only 720p as its "HD" stream, codecs vary (h264/h265/VP9), and the single `Size` field from MediathekViewWeb applies to only one URL tier but gets reported for all three. This causes Sonarr to make wrong upgrade decisions — it thinks it has 1080p when it only has 720p, so it never upgrades.

## What Changes

- **Quality probing service**: Three-phase detection that determines real resolution, codec, bitrate, and file size for each video URL — (1) URL pattern analysis (zero network cost), (2) HTTP HEAD for Content-Length and Content-Type, (3) optional partial download probe (first 32KB) to parse MP4 container headers.
- **Verified quality in search results**: Replace hardcoded quality tier assignment with probed quality data. Release titles reflect real resolution and codec (e.g., `720p.WEB.h265` instead of guessed `1080p.WEB.h264`).
- **Rich Newznab attributes**: Add missing `newznab:attr` elements to search XML responses — `resolution`, `video` (codec), `size` (real), `language`, `tvdbid`, `season`, `episode`.
- **Quality cache**: Cache probed quality per URL to avoid re-probing across searches. In-memory with configurable TTL.

## Capabilities

### New Capabilities
- `quality-probing`: Three-phase quality detection service — URL pattern analysis, HTTP HEAD probing, and optional container header parsing for resolution/codec/bitrate/size
- `quality-cache`: In-memory cache for probed quality data per URL with configurable TTL

### Modified Capabilities
- `mediathek-search`: Quality expansion must use probed quality data instead of hardcoded tier assumptions. Size estimation replaced by real Content-Length from HEAD requests.
- `newznab-indexer`: Search XML responses must include additional `newznab:attr` elements (resolution, video, size, language, tvdbid, season, episode) and release titles must reflect verified quality/codec.

## Impact

- **New service**: `QualityProbeService` with HTTP client for HEAD and Range requests
- **SearchActor** (`Search/SearchActor.cs`): Quality expansion refactored to use probed data
- **MatchingPipeline** (`Search/MatchingPipeline.cs`): Same quality expansion refactor, dedup with SearchActor
- **NewznabXmlBuilder** (`Indexer/NewznabXmlBuilder.cs`): Extended `newznab:attr` output, dynamic release title codec
- **SearchResult model** (`Shared/Models/SearchResult.cs`): Extended with verified quality fields
- **New dependency**: No external packages — uses built-in `HttpClient` and manual MP4 atom parsing
- **Network impact**: 1 HEAD request (~200B) per unique video URL per cache miss. Container probe (32KB) only for URLs where pattern analysis is inconclusive.
