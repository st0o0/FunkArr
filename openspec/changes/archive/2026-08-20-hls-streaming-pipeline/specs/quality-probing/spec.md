## ADDED Requirements

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

## MODIFIED Requirements

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
