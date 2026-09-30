## MODIFIED Requirements

### Requirement: Quality tiers
The system SHALL generate separate Newznab entries for each available quality tier using probed quality data. Release titles SHALL reflect the verified resolution, codec, and episode metadata from RuleSet matching.

#### Scenario: Verified quality in release title with episode info
- **WHEN** probing determines a video is 720p h265 and RuleSet matching resolved season=1, episode=3, episodeName="Köpfe"
- **THEN** the release title SHALL be `SHOW.S01E03.Koepfe.GERMAN.720p.WEB.h265-FA`

#### Scenario: Multiple verified qualities with episode info
- **WHEN** Url_Video_HD is probed as 1080p h265 and Url_Video as 720p h264, matched to S01E03 "Köpfe"
- **THEN** the Newznab RSS SHALL contain two entries: `SHOW.S01E03.Koepfe.GERMAN.1080p.WEB.h265-FA` and `SHOW.S01E03.Koepfe.GERMAN.720p.WEB.h264-FA`

#### Scenario: Estimated quality marked conservatively
- **WHEN** probing fails and quality is estimated
- **THEN** the release title SHALL use the conservative estimate (Url_Video_HD → 720p) rather than the optimistic guess (1080p)

### Requirement: Rich Newznab attributes
The system SHALL emit additional `newznab:attr` elements in search XML responses beyond the existing category and size attributes. Season and episode attributes SHALL be populated from RuleSet match data when available, falling back to HTTP query parameters.

#### Scenario: Resolution attribute
- **WHEN** probing determines resolution as 1920x1080
- **THEN** the XML SHALL include `<newznab:attr name="resolution" value="1080p" />`

#### Scenario: Video codec attribute
- **WHEN** probing determines codec as h265
- **THEN** the XML SHALL include `<newznab:attr name="video" value="h265" />`

#### Scenario: Real file size attribute
- **WHEN** probing determines file size as 1288490188 bytes
- **THEN** the XML SHALL include `<newznab:attr name="size" value="1288490188" />`

#### Scenario: Language attribute
- **WHEN** a search result is returned
- **THEN** the XML SHALL include `<newznab:attr name="language" value="German" />`

#### Scenario: TVDB ID attribute for TV searches
- **WHEN** a TV search result is returned for tvdbId 83214
- **THEN** the XML SHALL include `<newznab:attr name="tvdbid" value="83214" />`

#### Scenario: Season and episode attributes from match data
- **WHEN** a TV search result has ResolvedSeason=2 and ResolvedEpisode=5 from RuleSet matching
- **THEN** the XML SHALL include `<newznab:attr name="season" value="2" />` and `<newznab:attr name="episode" value="5" />`

#### Scenario: Season and episode from HTTP params as fallback
- **WHEN** a TV search result has no resolved episode data but HTTP params season=1 and ep=3 were provided
- **THEN** the XML SHALL include `<newznab:attr name="season" value="1" />` and `<newznab:attr name="episode" value="3" />`

#### Scenario: Estimated quality — no resolution attribute
- **WHEN** quality is estimated (ProbeSource=Estimated) rather than verified
- **THEN** the XML SHALL NOT include a resolution attribute (to avoid misleading Sonarr)

### Requirement: Controller-based implementation
The Newznab API SHALL be implemented as a controller mapped to `/index/api`. The controller SHALL use `NewznabSerializer` with declarative XML models instead of imperative XmlWriter. The controller SHALL map SearchResult directly to `NewznabRssItem` without an intermediate `NewznabResult` DTO.

#### Scenario: TV search response
- **WHEN** a TV search for tvdbId 12345 returns 3 matched episodes
- **THEN** the controller SHALL serialize a `NewznabRssFeed` with 3 items, each with enriched release titles

#### Scenario: Text search response with fallback titles
- **WHEN** a text search returns results without RuleSet matching
- **THEN** the controller SHALL use fallback release titles including episode title and air date from SearchResult

#### Scenario: Browse/RSS response
- **WHEN** the RSS feed endpoint is queried without search parameters
- **THEN** all results SHALL have fallback release titles with episode title and air date differentiation
