## MODIFIED Requirements

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
- **THEN** the service SHALL return an estimated QualityInfo based on the URL tier (Url_Video_HD -> HD1080, Url_Video -> HD720, Url_Video_Low -> SD) with ProbeSource=Estimated

#### Scenario: HD URL fallback tier
- **WHEN** `ExpandWithProbingAsync` processes `Url_Video_HD` and probing falls back to estimation
- **THEN** the fallback tier SHALL be `HD1080`, consistent with the sync expansion path
