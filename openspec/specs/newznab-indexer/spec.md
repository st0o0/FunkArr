## MODIFIED Requirements

### Requirement: Quality tiers
The system SHALL generate separate Newznab entries for each available quality tier using probed quality data. Release titles SHALL reflect the verified resolution and codec.

#### Scenario: Verified quality in release title
- **WHEN** probing determines a video is 720p h265
- **THEN** the release title SHALL be `SHOW.S01E03.GERMAN.720p.WEB.h265-FA` (not hardcoded h264)

#### Scenario: Multiple verified qualities
- **WHEN** Url_Video_HD is probed as 1080p h265 and Url_Video as 720p h264
- **THEN** the Newznab RSS SHALL contain two entries: `SHOW.S01E03.GERMAN.1080p.WEB.h265-FA` and `SHOW.S01E03.GERMAN.720p.WEB.h264-FA`

#### Scenario: Estimated quality marked conservatively
- **WHEN** probing fails and quality is estimated
- **THEN** the release title SHALL use the conservative estimate (Url_Video_HD → 720p) rather than the optimistic guess (1080p)

### Requirement: Rich Newznab attributes
The system SHALL emit additional `newznab:attr` elements in search XML responses beyond the existing category and size attributes.

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

#### Scenario: Season and episode attributes
- **WHEN** a TV search result matches season 1 episode 5
- **THEN** the XML SHALL include `<newznab:attr name="season" value="1" />` and `<newznab:attr name="episode" value="5" />`

#### Scenario: Estimated quality — no resolution attribute
- **WHEN** quality is estimated (ProbeSource=Estimated) rather than verified
- **THEN** the XML SHALL NOT include a resolution attribute (to avoid misleading Sonarr)

### Requirement: API key validation
The system SHALL validate the `apikey` query parameter on all Newznab endpoints. Requests with missing or invalid API keys MUST receive a Newznab error response.

#### Scenario: Missing API key
- **WHEN** a client sends `GET /api?t=tvsearch&tvdbid=12345` without an `apikey` parameter
- **THEN** the system returns HTTP 200 with Newznab error XML (code 100, "Incorrect user credentials")

#### Scenario: Valid API key
- **WHEN** a client sends a request with a valid `apikey`
- **THEN** the request is processed normally
