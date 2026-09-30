## MODIFIED Requirements

### Requirement: Quality expansion with probed data
The system SHALL replace hardcoded quality tier assignment with probed quality data from the QualityProbeService. Each non-empty video URL SHALL be probed (or served from cache) to determine real resolution, codec, and file size.

#### Scenario: Probed quality replaces hardcoded tier
- **WHEN** Url_Video_HD is probed and returns resolution=1280x720, codec=h264
- **THEN** the search result SHALL report QualityTier=HD720 (not hardcoded HD1080)

#### Scenario: Multiple qualities with real data
- **WHEN** a Mediathek result has Url_Video_HD (probed: 720p) and Url_Video (probed: 480p)
- **THEN** two search results SHALL be created with their real quality tiers

#### Scenario: Probe failure falls back to estimation
- **WHEN** probing fails for Url_Video_HD
- **THEN** the search result SHALL use estimated quality (HD720 conservative default) with ProbeSource=Estimated

### Requirement: Real file size in search results
The system SHALL use the Content-Length from HTTP HEAD (Phase 2) as the file size instead of the single Size field from MediathekViewWeb or fixed-bitrate estimation.

#### Scenario: HEAD provides real size
- **WHEN** HEAD request returns Content-Length=1288490188 for Url_Video_HD
- **THEN** the search result SHALL report SizeBytes=1288490188

#### Scenario: HEAD unavailable — estimate from probed bitrate
- **WHEN** HEAD fails but Phase 1 extracted bitrate=2256kbps and duration is 2700s
- **THEN** the search result SHALL estimate size as 2256*2700*1000/8 bytes

### Requirement: Consolidated quality expansion
The quality expansion logic SHALL exist in a single location (via QualityProbeService) instead of being duplicated between MatchingPipeline and SearchActor.

#### Scenario: Single code path
- **WHEN** quality expansion is needed in either the generic pipeline or the ruleset-based flow
- **THEN** both paths SHALL call the same QualityProbeService method

### Requirement: Probe scope limiting
The system SHALL only probe quality for the top N matched results (configurable, default 30) to bound search latency.

#### Scenario: Probe top results only
- **WHEN** matching produces 50 results before quality expansion
- **THEN** only the top 30 by match score SHALL be probed, the rest SHALL use estimation

#### Scenario: Configurable probe limit
- **WHEN** `FunkArr__QualityProbeLimit` is set to 10
- **THEN** only the top 10 results SHALL be probed
