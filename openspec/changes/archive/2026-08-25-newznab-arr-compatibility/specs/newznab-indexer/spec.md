## ADDED Requirements

### Requirement: Pagination for TV and Movie searches
The system SHALL apply `limit` and `offset` pagination to TV search and Movie search responses, matching the existing behavior of Text search. Default limit SHALL be 100 and default offset SHALL be 0.

#### Scenario: TV search with limit and offset
- **WHEN** a client sends `t=tvsearch&tvdbid=329324&limit=10&offset=20`
- **THEN** the system SHALL skip the first 20 results and return at most 10 results

#### Scenario: Movie search with limit and offset
- **WHEN** a client sends `t=movie&imdbid=tt0082096&limit=5&offset=0`
- **THEN** the system SHALL return at most 5 results starting from the first

#### Scenario: Default pagination when not specified
- **WHEN** a client sends `t=tvsearch&tvdbid=329324` without limit or offset
- **THEN** the system SHALL default to limit=100 and offset=0

### Requirement: Accurate response offset and total
The `<newznab:response>` element SHALL report the actual `offset` from the request and the `total` count of all available results (before pagination), not the page count.

#### Scenario: Paginated response metadata
- **WHEN** a search returns 85 total results and the client requested offset=20 and limit=10
- **THEN** the response SHALL contain `<newznab:response offset="20" total="85" />`

#### Scenario: Full result set response metadata
- **WHEN** a search returns 30 results and the client requested no offset or limit
- **THEN** the response SHALL contain `<newznab:response offset="0" total="30" />`

## MODIFIED Requirements

### Requirement: Rich Newznab attributes
The system SHALL emit additional `newznab:attr` elements in search XML responses beyond the existing category and size attributes. Season and episode attributes SHALL be populated from RuleSet match data when available, falling back to HTTP query parameters. The system SHALL also emit `imdbid`, `year`, and `tvdbid` attributes from pipeline-resolved data when available.

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

#### Scenario: TVDB ID attribute from pipeline resolution
- **WHEN** a TV search is performed by query (`t=tvsearch&q=Tatort`) and the ShowActor resolves tvdbId 83214
- **THEN** the XML SHALL include `<newznab:attr name="tvdbid" value="83214" />` even though the caller did not send a tvdbid parameter

#### Scenario: Season and episode attributes from match data
- **WHEN** a TV search result has ResolvedSeason=2 and ResolvedEpisode=5 from RuleSet matching
- **THEN** the XML SHALL include `<newznab:attr name="season" value="2" />` and `<newznab:attr name="episode" value="5" />`

#### Scenario: Season and episode from HTTP params as fallback
- **WHEN** a TV search result has no resolved episode data but HTTP params season=1 and ep=3 were provided
- **THEN** the XML SHALL include `<newznab:attr name="season" value="1" />` and `<newznab:attr name="episode" value="3" />`

#### Scenario: Estimated quality — no resolution attribute
- **WHEN** quality is estimated (ProbeSource=Estimated) rather than verified
- **THEN** the XML SHALL NOT include a resolution attribute (to avoid misleading Sonarr)

#### Scenario: IMDb ID attribute for movie searches
- **WHEN** a movie search result has ImdbId "tt0082096" resolved from the pipeline
- **THEN** the XML SHALL include `<newznab:attr name="imdbid" value="tt0082096" />`

#### Scenario: IMDb ID from query-based movie search
- **WHEN** a movie search is performed by query (`t=movie&q=Das Boot`) and the pipeline resolves ImdbId "tt0082096"
- **THEN** the XML SHALL include `<newznab:attr name="imdbid" value="tt0082096" />`

#### Scenario: Year attribute for movie searches
- **WHEN** a movie search result has Year 1981 resolved from TMDB
- **THEN** the XML SHALL include `<newznab:attr name="year" value="1981" />`

#### Scenario: Year absent when not resolved
- **WHEN** a search result has no resolved year (e.g., TV search or failed TMDB lookup)
- **THEN** the XML SHALL NOT include a `year` attribute
