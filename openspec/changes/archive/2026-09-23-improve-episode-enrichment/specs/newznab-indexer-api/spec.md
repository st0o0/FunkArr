## MODIFIED Requirements

### Requirement: Search pagination parameters
The system SHALL accept `offset` (int, default 0) and `limit` (int, default 100) query parameters on all search endpoints. Paging SHALL be applied exactly once in `NewznabSearchService` after retrieving cached results and after any season/episode filtering. The RSS mapper SHALL NOT apply additional paging.

#### Scenario: Paging applied once in service
- **WHEN** `?t=tvsearch&q=Tatort&offset=20&limit=25` is requested
- **AND** the cache contains 100 total results
- **THEN** `NewznabSearchService` SHALL apply `Skip(20).Take(25)` to get items 20-44
- **AND** the RSS mapper SHALL receive exactly those 25 items and map them without further skip/take

#### Scenario: RSS response pagination metadata
- **WHEN** search results are returned with offset=20 and 100 total items
- **THEN** the RSS response SHALL have `<newznab:response offset="20" total="100"/>`

#### Scenario: TV search builds SearchCommand with TvParams
- **WHEN** `?t=tvsearch&q=Tatort&season=01&ep=05&tvdbid=83214` is requested
- **THEN** the SearchHandler SHALL build `SearchCommand(Query: "Tatort", Cat: null, Limit: null, Offset: null, Params: TvParams(Season: 1, Episode: 5, TvdbId: 83214, ImdbId: null))`

#### Scenario: Movie search builds SearchCommand with MovieParams
- **WHEN** `?t=movie&imdbid=tt0806910` is requested
- **THEN** the SearchHandler SHALL build `SearchCommand(Query: null, Cat: null, Limit: null, Offset: null, Params: MovieParams(ImdbId: "tt0806910", TmdbId: null))`

#### Scenario: General search builds SearchCommand without type params
- **WHEN** `?t=search&q=Tatort&cat=5040` is requested
- **THEN** the SearchHandler SHALL build `SearchCommand(Query: "Tatort", Cat: 5040, Limit: null, Offset: null, Params: null)`

#### Scenario: Limit exceeds max
- **WHEN** `?t=tvsearch&q=Tatort&limit=1000` is requested
- **THEN** the system SHALL cap limit to 500 before forwarding to the search pipeline

#### Scenario: Default pagination
- **WHEN** a search request omits `offset` and `limit`
- **THEN** the system SHALL forward Limit=null and Offset=null (workers apply their own defaults)

### Requirement: SearchResultCache cleanup
`SearchResultCache.GetOrAddAsync` SHALL remove pending entries in a `finally` block only. There SHALL be no redundant removal in a `catch` block. The cache key for TV searches SHALL be built from query, tvdbid, and imdbid only — season and episode SHALL NOT be part of the cache key because the Mediathek returns the same results regardless of the requested episode.

#### Scenario: Pending entry removed after success
- **WHEN** `GetOrAddAsync` completes successfully
- **THEN** the pending entry SHALL be removed from the `_pending` dictionary

#### Scenario: Pending entry removed after failure
- **WHEN** `GetOrAddAsync` throws an exception
- **THEN** the pending entry SHALL be removed from the `_pending` dictionary via the `finally` block
- **AND** the exception SHALL be rethrown

#### Scenario: TV search cache key excludes season and episode
- **WHEN** two TV searches are made for the same series but different episodes (`season=2026&ep=17` then `season=2026&ep=18`)
- **THEN** both SHALL use the same cache key (`tv:{query}:{tvdbid}:{imdbid}`)
- **AND** the second search SHALL return cached results from the first

#### Scenario: Different series use different cache keys
- **WHEN** TV searches are made for different series (different tvdbid)
- **THEN** they SHALL use different cache keys and not share cached results

## ADDED Requirements

### Requirement: Newznab TV search filters results by season and episode
When a TV search request includes both `season` and `ep` parameters and the cached results contain items with enriched season/episode newznab attributes, `NewznabSearchService` SHALL filter the results to only include items whose `season` and `episode` attributes match the requested values. Items without season/episode attributes SHALL be excluded when specific season/episode filtering is active. Filtering SHALL occur after cache retrieval and before pagination.

#### Scenario: Filter returns only matching episode
- **WHEN** `?t=tvsearch&q=Tatort&season=2026&ep=17` is requested
- **AND** cached results contain items with season=2026/episode=17, season=2026/episode=18, and items without season/episode
- **THEN** only items with season="2026" AND episode="17" SHALL be returned

#### Scenario: Unmatched items excluded when filtering
- **WHEN** `?t=tvsearch&q=Tatort&season=2026&ep=17` is requested
- **AND** some cached items have no season/episode attributes
- **THEN** those items SHALL NOT be included in the response

#### Scenario: No season/episode means no filtering
- **WHEN** `?t=tvsearch&q=Tatort` is requested without season or ep parameters
- **THEN** all cached results SHALL be returned (no filtering applied)

#### Scenario: Only season provided without episode
- **WHEN** `?t=tvsearch&q=Tatort&season=2026` is requested without ep
- **THEN** results SHALL be filtered to items with season="2026" (any episode)
- **AND** items without season attribute SHALL be excluded

#### Scenario: Pagination applied after filtering
- **WHEN** `?t=tvsearch&q=Tatort&season=2026&ep=17&offset=0&limit=10` is requested
- **AND** filtering reduces 150 cached results to 6 matching items
- **THEN** the response SHALL contain 6 items with `total="6"`

#### Scenario: RSS sync without episode returns all
- **WHEN** Sonarr performs an RSS sync via `?t=tvsearch&tvdbid=83214` without season or ep
- **THEN** all cached results SHALL be returned unfiltered
