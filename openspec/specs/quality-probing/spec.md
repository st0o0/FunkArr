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

#### Scenario: HLS URL detection
- **WHEN** the URL ends with `.m3u8` or Content-Type is `application/x-mpegURL`
- **THEN** the service SHALL mark the URL as HLS and skip Phase 3 (container probing not applicable)

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

### Requirement: Probing orchestration
The QualityProbeService SHALL execute phases in order, stopping when sufficient quality data is obtained.

#### Scenario: Phase 1 resolves everything
- **WHEN** URL pattern analysis determines resolution, codec, and bitrate
- **THEN** only Phase 2 SHALL execute (for file size), Phase 3 SHALL be skipped

#### Scenario: Phase 1 provides partial data
- **WHEN** URL pattern analysis determines resolution but not codec
- **THEN** Phase 2 SHALL execute, and Phase 3 SHALL execute if the container is MP4

#### Scenario: All phases fail
- **WHEN** no phase produces definitive quality data
- **THEN** the service SHALL return an estimated QualityInfo based on the URL tier (Url_Video_HD → HD720 estimated, maintaining current conservative behavior) with ProbeSource=Estimated

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
- **WHEN** `FunkArr__QualityProbing` is set to `false`
- **THEN** the service SHALL skip all phases and return estimated QualityInfo (current behavior)
