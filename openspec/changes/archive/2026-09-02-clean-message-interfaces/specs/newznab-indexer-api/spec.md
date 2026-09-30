## MODIFIED Requirements

### Requirement: Search pagination parameters

The system SHALL accept `offset` (int, default 0) and `limit` (int, default 100) query parameters on all search endpoints (`t=search`, `t=tvsearch`, `t=movie`). The system SHALL cap `limit` to the Caps-advertised max (500) before forwarding to the search pipeline. The SearchHandler SHALL build a unified `SearchCommand` for all search types.

#### Scenario: TV search builds SearchCommand with TvParams

- **WHEN** `?t=tvsearch&q=Tatort&season=01&ep=05&tvdbid=83214` is requested
- **THEN** the SearchHandler SHALL build `SearchCommand(Query: "Tatort", Cat: null, Limit: null, Offset: null, Params: TvParams(Season: 1, Episode: 5, TvdbId: 83214, ImdbId: null))`

#### Scenario: Movie search builds SearchCommand with MovieParams

- **WHEN** `?t=movie&imdbid=tt0806910` is requested
- **THEN** the SearchHandler SHALL build `SearchCommand(Query: null, Cat: null, Limit: null, Offset: null, Params: MovieParams(ImdbId: "tt0806910", TmdbId: null))`

#### Scenario: General search builds SearchCommand without type params

- **WHEN** `?t=search&q=Tatort&cat=5040` is requested
- **THEN** the SearchHandler SHALL build `SearchCommand(Query: "Tatort", Cat: 5040, Limit: null, Offset: null, Params: null)`

#### Scenario: Pagination parameters forwarded

- **WHEN** `?t=tvsearch&q=Tatort&offset=10&limit=25` is requested
- **THEN** the system SHALL forward Limit=25 and Offset=10 in the SearchCommand

#### Scenario: Default pagination

- **WHEN** a search request omits `offset` and `limit`
- **THEN** the system SHALL forward Limit=null and Offset=null (workers apply their own defaults)

#### Scenario: Limit exceeds max

- **WHEN** `?t=tvsearch&q=Tatort&limit=1000` is requested
- **THEN** the system SHALL cap limit to 500 before forwarding to the search pipeline

#### Scenario: RSS response pagination

- **WHEN** search results are returned with offset=10
- **THEN** the RSS response SHALL have `<newznab:response offset="10" total="N"/>` where N is the total number of matching items
