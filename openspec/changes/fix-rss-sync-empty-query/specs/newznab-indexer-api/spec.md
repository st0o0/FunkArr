## ADDED Requirements

### Requirement: Search requests without search criteria
A `t=tvsearch`, `t=movie` or `t=search` request SHALL be treated as a request without search criteria when `q` is empty or whitespace and none of `tvdbid`, `imdbid`, `tmdbid` is present. `season` and `ep` alone SHALL NOT count as search criteria. Such a request SHALL NOT trigger a search against MediathekViewWeb.

#### Scenario: Sonarr RSS sync request
- **WHEN** `GET /index/api?t=tvsearch&cat=5030,5040&extended=1&offset=0&limit=100` is requested
- **THEN** the request SHALL be treated as a request without search criteria
- **AND** no MediathekViewWeb query SHALL be made

#### Scenario: Radarr RSS sync request
- **WHEN** `GET /index/api?t=movie&cat=2000,2040&offset=0&limit=100` is requested
- **THEN** the request SHALL be treated as a request without search criteria
- **AND** the response SHALL NOT be Newznab error XML

#### Scenario: Whitespace query
- **WHEN** `GET /index/api?t=tvsearch&q=%20%20` is requested without any media ID
- **THEN** the request SHALL be treated as a request without search criteria

#### Scenario: Season without query or ID
- **WHEN** `GET /index/api?t=tvsearch&season=1&ep=2` is requested without `q` and without any media ID
- **THEN** the request SHALL be treated as a request without search criteria

#### Scenario: Media ID without query is a real search
- **WHEN** `GET /index/api?t=tvsearch&tvdbid=83214` is requested without `q`
- **THEN** the request SHALL be processed as a search, unchanged from current behavior

#### Scenario: Query is a real search
- **WHEN** `GET /index/api?t=search&q=Tatort` is requested
- **THEN** the request SHALL be processed as a search, unchanged from current behavior

### Requirement: Validation item for requests without search criteria
A request without search criteria SHALL be answered with valid Newznab RSS XML containing exactly one validation item, so that *arr indexer tests succeed. The item's category SHALL match the request: TV for `t=tvsearch`, Movies for `t=movie`, and for `t=search` Movies only when every requested category is a movie category, otherwise TV.

#### Scenario: TV validation item
- **WHEN** `GET /index/api?t=tvsearch&offset=0&limit=100` is requested
- **THEN** the response SHALL contain exactly one item with `newznab:attr name="category" value="5040"`
- **AND** `<newznab:response offset="0" total="1"/>`

#### Scenario: Movie validation item
- **WHEN** `GET /index/api?t=movie&offset=0&limit=100` is requested
- **THEN** the response SHALL contain exactly one item with `newznab:attr name="category" value="2040"`

#### Scenario: General search with movie categories only
- **WHEN** `GET /index/api?t=search&cat=2000,2040` is requested without `q`
- **THEN** the validation item SHALL be in category 2040

#### Scenario: General search with mixed or missing categories
- **WHEN** `GET /index/api?t=search&cat=2040,5040` or `GET /index/api?t=search` is requested without `q`
- **THEN** the validation item SHALL be in category 5040

#### Scenario: Paging beyond the validation item
- **WHEN** a request without search criteria uses `offset=1` or higher
- **THEN** the response SHALL contain zero items and `<newznab:response offset="<offset>" total="1"/>`

### Requirement: Validation item cannot cause a download
The validation item SHALL have a title that is recognizable as a FunkArr validation entry, a stable GUID, a positive size, and a `pubDate` set to the time of the request. Its `get` link SHALL carry an ID that `t=get` rejects with Newznab error code 201, so that a grab can never start a download.

#### Scenario: Validation item content
- **WHEN** a request without search criteria is answered
- **THEN** the item title SHALL start with `FunkArr.Validation.`
- **AND** the GUID SHALL be identical across requests of the same category
- **AND** the item SHALL have a `size` attribute greater than zero

#### Scenario: Grabbing the validation item
- **WHEN** the `get` link of the validation item is requested
- **THEN** the system SHALL return Newznab error XML with code 201 (incorrect parameter)
- **AND** no NZB SHALL be returned
