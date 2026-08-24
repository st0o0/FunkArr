## REMOVED Requirements

### Requirement: QualityProbeWorker actor
**Reason**: HTTP-based quality probing (HEAD requests for Content-Length, Range requests for MP4 atom parsing) is redundant. MediathekViewWeb URLs encode resolution, codec, and bitrate in their filenames (e.g., `.avc-1080.mp4`, `1920x1080-50p-5000kbit.mp4`). The existing `UrlPatternAnalyzer` extracts this information without any HTTP calls. File size estimation via bitrate × duration is sufficient for Sonarr/Radarr quality decisions.
**Migration**: Replace `QualityProbeWorker.Ask<UrlsProbed>(ProbeUrls(...))` with inline calls to `UrlPatternAnalyzer.Analyze(url)` + `QualityProbeService.EstimateSize(duration, tier)` within search pipeline entities. The `QualityProbeService` class is retained for its static estimation methods but HTTP probing methods are removed.

### Requirement: Quality probe caching
**Reason**: URL pattern analysis is a pure CPU function (<1ms) that produces deterministic results. No caching is needed — the same URL always produces the same quality info.
**Migration**: Remove `QualityProbeService._cache` and related cache infrastructure.

### Requirement: Container header probing
**Reason**: MP4 atom parsing via HTTP Range requests provided resolution and codec from the video container header. This is redundant when URL patterns already contain the same information for all known CDN URL formats (ARD, ZDF, Arte).
**Migration**: Remove `ProbeContainerAsync`, `Mp4AtomParser` usage from the search pipeline. `Mp4AtomParser` can be retained as a utility if needed elsewhere.

### Requirement: HLS manifest probing
**Reason**: No HLS-only content observed in MediathekViewWeb data — all entries have MP4 URLs alongside any HLS variants. URL pattern analysis handles MP4 URLs.
**Migration**: Remove `ProbeHlsManifestAsync` from the search pipeline. `HlsManifestParser` is retained for the download pipeline (HLS downloads still need manifest parsing).
