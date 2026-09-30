## ADDED Requirements

### Requirement: Quality variant expansion
The QualityExpander SHALL expand a `MediathekResultItem` into up to three `SearchResult` records, one per available quality tier (HD1080, HD720, SD), based on the item's URL fields (`UrlVideoHd`, `UrlVideo`, `UrlVideoLow`). Empty or null URLs SHALL be skipped.

#### Scenario: All three qualities available
- **WHEN** a MediathekResultItem has non-empty UrlVideoHd, UrlVideo, and UrlVideoLow
- **THEN** the expander SHALL produce three SearchResult records with qualities HD1080, HD720, and SD

#### Scenario: Only standard quality available
- **WHEN** a MediathekResultItem has only a non-empty UrlVideo
- **THEN** the expander SHALL produce one SearchResult with quality HD720

#### Scenario: Empty URL skipped
- **WHEN** a MediathekResultItem has UrlVideoHd = ""
- **THEN** no HD1080 SearchResult SHALL be produced

### Requirement: URL pattern analysis for quality detection
The QualityExpander SHALL use `UrlPatternAnalyzer.Analyze()` to detect the actual resolution from URL patterns. When a resolution is detected, it SHALL override the fallback quality tier.

#### Scenario: URL pattern indicates 1080p
- **WHEN** UrlPatternAnalyzer detects resolution height >= 1080 for UrlVideo
- **THEN** the SearchResult SHALL have quality HD1080 instead of the fallback HD720

#### Scenario: No URL pattern detected
- **WHEN** UrlPatternAnalyzer returns no resolution for a URL
- **THEN** the SearchResult SHALL use the fallback quality tier for that URL field

### Requirement: Size estimation
The QualityExpander SHALL estimate file size using bitrate from URL pattern analysis when available, falling back to `QualityProbeService.EstimateSize()` when no bitrate is detected.

#### Scenario: Bitrate-based size estimation
- **WHEN** UrlPatternAnalyzer detects a bitrate of 3500 kbps and item duration is 3600 seconds
- **THEN** SizeBytes SHALL be `3600 * 3500 * 1000 / 8`

#### Scenario: Fallback size estimation
- **WHEN** UrlPatternAnalyzer detects no bitrate
- **THEN** SizeBytes SHALL use `QualityProbeService.EstimateSize(duration, qualityTier)`

### Requirement: SearchResult field mapping
The QualityExpander SHALL map MediathekResultItem fields to SearchResult fields: Title, Topic, Channel, Url, UrlSubtitle (null if empty), DurationSeconds, Timestamp (from epoch seconds), Description (null if empty), Quality, and SizeBytes.

#### Scenario: Subtitle URL mapping
- **WHEN** a MediathekResultItem has UrlSubtitle = ""
- **THEN** the SearchResult SHALL have UrlSubtitle = null

#### Scenario: Timestamp conversion
- **WHEN** a MediathekResultItem has Timestamp = 1719244800
- **THEN** the SearchResult SHALL have Timestamp = DateTimeOffset corresponding to that Unix epoch
