## ADDED Requirements

### Requirement: URL pattern analysis (Phase 1)
The QualityProbeService SHALL analyze video URLs to extract quality hints from the URL path without making any network requests.

#### Scenario: ZDF bitrate and profile in URL
- **WHEN** the URL contains `2256k_p18v17.mp4`
- **THEN** Phase 1 SHALL extract bitrate=2256kbps and map profile p18 to 720p h264

#### Scenario: ZDF high-quality profile
- **WHEN** the URL contains `6660k_p37v17.mp4`
- **THEN** Phase 1 SHALL extract bitrate=6660kbps and map profile p37 to 1080p h265

#### Scenario: ARD resolution in path
- **WHEN** the URL contains `/720/` in the path
- **THEN** Phase 1 SHALL extract resolution as 720p

#### Scenario: No pattern match
- **WHEN** the URL does not match any known broadcaster pattern
- **THEN** Phase 1 SHALL return null, signaling that Phase 2/3 are needed

### Requirement: HTTP HEAD probing (Phase 2)
The QualityProbeService SHALL send HTTP HEAD requests to video URLs to obtain Content-Length and Content-Type.

#### Scenario: Successful HEAD request
- **WHEN** a HEAD request to a video URL returns 200 with Content-Length=1288490188 and Content-Type=video/mp4
- **THEN** Phase 2 SHALL record fileSize=1288490188 and container=mp4

#### Scenario: HEAD request fails
- **WHEN** a HEAD request returns 403, 405, or times out
- **THEN** Phase 2 SHALL return null for size and container, falling back to estimation

#### Scenario: HEAD request timeout
- **WHEN** a HEAD request does not respond within 5 seconds
- **THEN** the service SHALL cancel the request and fall back to estimation

### Requirement: Container header probing (Phase 3)
The QualityProbeService SHALL optionally download the first 32KB of a video file via HTTP Range request and parse the container header to extract resolution, codec, and bitrate.

#### Scenario: MP4 with faststart moov atom
- **WHEN** a Range request for bytes 0-32767 returns MP4 data with the moov atom in the first 32KB
- **THEN** Phase 3 SHALL parse the video track to extract width=1920, height=1080, codec=h264

#### Scenario: MP4 with moov at end of file
- **WHEN** the moov atom is not within the first 32KB
- **THEN** Phase 3 SHALL return null (fallback to Phase 1 + 2 data)

#### Scenario: Range request not supported
- **WHEN** the server responds with 200 (full content) instead of 206 (partial content)
- **THEN** Phase 3 SHALL abort and return null

#### Scenario: WebM container
- **WHEN** the Content-Type is video/webm
- **THEN** Phase 3 SHALL skip container parsing (EBML not supported) and use Phase 1 + 2 data

### Requirement: HLS manifest quality probing
The QualityProbeService SHALL parse HLS master manifests to extract quality information for .m3u8 URLs. This replaces HTTP HEAD and container probing for HLS content.

#### Scenario: Master manifest with resolution and bandwidth
- **WHEN** an HLS master manifest contains `#EXT-X-STREAM-INF:BANDWIDTH=5000000,RESOLUTION=1920x1080`
- **THEN** the probe SHALL extract resolution=1920x1080, tier=HD1080, bitrate=5000kbps, probeSource=HlsManifest

#### Scenario: Master manifest with multiple quality tiers
- **WHEN** an HLS master manifest contains multiple `EXT-X-STREAM-INF` lines with different resolutions
- **THEN** the probe SHALL use the highest resolution variant for the quality info

#### Scenario: Master manifest without resolution attribute
- **WHEN** an HLS master manifest has `EXT-X-STREAM-INF` with `BANDWIDTH` but no `RESOLUTION`
- **THEN** the probe SHALL estimate resolution from bandwidth (>4000kbps -> HD1080, >2000kbps -> HD720, else SD)

#### Scenario: Manifest fetch fails
- **WHEN** the HTTP GET for the .m3u8 manifest fails or times out
- **THEN** the probe SHALL fall back to estimated quality based on URL tier with probeSource=Estimated

### Requirement: HLS URL detection
The QualityProbeService SHALL detect HLS URLs and route them to manifest-based probing instead of HTTP HEAD / container probing.

#### Scenario: HLS URL detection
- **WHEN** the URL ends with `.m3u8` or Content-Type is `application/x-mpegURL`
- **THEN** the service SHALL use HLS manifest probing (Phase M) instead of Phase 2 (HEAD) and Phase 3 (container), while Phase 1 (URL pattern) still runs first

### Requirement: Probing orchestration
The QualityProbeService SHALL execute phases in order, stopping when sufficient quality data is obtained. For HLS URLs, the phase order is Phase 1 (URL pattern) then Phase M (manifest parsing).

#### Scenario: HLS URL with Phase 1 match
- **WHEN** a .m3u8 URL matches a known broadcaster pattern in Phase 1
- **THEN** Phase M is skipped and the Phase 1 result is used

#### Scenario: HLS URL without Phase 1 match
- **WHEN** a .m3u8 URL does not match any known broadcaster pattern
- **THEN** Phase M fetches and parses the master manifest to determine quality

#### Scenario: Phase 1 resolves everything
- **WHEN** URL pattern analysis determines resolution, codec, and bitrate
- **THEN** only Phase 2 SHALL execute (for file size), Phase 3 SHALL be skipped

#### Scenario: Phase 1 provides partial data
- **WHEN** URL pattern analysis determines resolution but not codec
- **THEN** Phase 2 SHALL execute, and Phase 3 SHALL execute if the container is MP4

#### Scenario: All phases fail
- **WHEN** no phase produces definitive quality data
- **THEN** the service SHALL return an estimated QualityInfo based on the URL tier (Url_Video_HD -> HD720 estimated, maintaining current conservative behavior) with ProbeSource=Estimated

### Requirement: QualityInfo result structure
The probe result SHALL be a QualityInfo record containing resolution (width × height), quality tier (derived), codec (string), bitrate (nullable kbps), file size (bytes), container format, and probe source indicator.

#### Scenario: Fully probed result
- **WHEN** probing succeeds through Phase 3
- **THEN** QualityInfo SHALL contain resolution=1920x1080, tier=HD1080, codec="h265", bitrate=6660, fileSize=2147483648, container="mp4", probeSource=ContainerHeader

#### Scenario: HEAD-only result
- **WHEN** Phase 1 determines resolution and codec but Phase 3 is skipped
- **THEN** QualityInfo SHALL contain the Phase 1 resolution/codec with Phase 2 fileSize, probeSource=UrlPattern

#### Scenario: Quality tier derivation
- **WHEN** resolution height is determined
- **THEN** tier SHALL be: height >= 1080 → HD1080, height >= 720 → HD720, else SD

### Requirement: Graceful degradation
The QualityProbeService SHALL never block or fail a search result because probing fails. Any probe failure SHALL fall back to estimated quality data.

#### Scenario: Network error during probing
- **WHEN** all HTTP requests fail (DNS, timeout, connection refused)
- **THEN** the service SHALL return estimated QualityInfo and log a warning

#### Scenario: Probe disabled by configuration
- **WHEN** `FunkArr__Quality__Probing` is set to `false`
- **THEN** the service SHALL skip all phases and return estimated QualityInfo (current behavior)

### Requirement: QualityProbeWorker actor wrapping QualityProbeService
`QualityProbeWorker` SHALL be a permanent child of `SearchCoordinator` that wraps `QualityProbeService`. It SHALL respond to `ProbeUrls(SearchResult[])` with `UrlsProbed(SearchResult[])`.

#### Scenario: Probe request via Ask
- **WHEN** `SearchCoordinator` asks `QualityProbeWorker` with `ProbeUrls(results)`
- **THEN** `QualityProbeWorker` SHALL invoke `QualityProbeService.ExpandWithProbingAsync` and reply with `UrlsProbed(enrichedResults)`

### Requirement: In-memory probe cache with deduplication
`QualityProbeWorker` SHALL maintain an in-memory `Dictionary<string, QualityInfo>` cache keyed by URL. Duplicate probe requests for the same URL SHALL return cached data.

#### Scenario: URL already probed
- **WHEN** `QualityProbeWorker` receives a `ProbeUrls` batch containing a URL that was previously probed
- **THEN** it SHALL use the cached `QualityInfo` for that URL without making a network request

#### Scenario: Inflight deduplication
- **WHEN** two concurrent `ProbeUrls` batches contain the same URL
- **THEN** only one probe request SHALL be made for that URL

### Requirement: Tier 2 event-sourced persistence
`QualityProbeWorker` SHALL be a `ReceivePersistentActor` with `PersistenceId: "quality-probe"`. It SHALL persist `UrlProbed(url, qualityInfo)` events. Snapshots SHALL be taken every 500 events.

#### Scenario: Cache warm on recovery
- **WHEN** `QualityProbeWorker` restarts after a crash
- **THEN** it SHALL recover its probe cache from the latest snapshot + replayed events

#### Scenario: Snapshot every 500 events
- **WHEN** 500 probe events have been persisted since the last snapshot
- **THEN** `QualityProbeWorker` SHALL save a snapshot of the current cache state
