## MODIFIED Requirements

### Requirement: NewznabApiEndpoints resolves dependencies via DI
`NewznabController` SHALL be an `[ApiController]` with constructor-injected `NewznabSearchService`, `NzbService`, and `ILogger<NewznabController>`. The controller SHALL dispatch requests based on the `t` query parameter to the appropriate service method. The controller SHALL NOT instantiate services inline or resolve them from `HttpContext.RequestServices`.

#### Scenario: Controller receives services via constructor
- **WHEN** `NewznabController` is instantiated by the DI container
- **THEN** it SHALL receive `NewznabSearchService`, `NzbService`, and `ILogger<NewznabController>` via constructor injection

#### Scenario: Search dispatched to service
- **WHEN** a request with `?t=tvsearch`, `?t=movie`, or `?t=search` arrives
- **THEN** the controller SHALL delegate to `NewznabSearchService.Search(request)` and return the result

#### Scenario: NZB get dispatched to service
- **WHEN** a request with `?t=get&id=<guid>` arrives
- **THEN** the controller SHALL delegate to `NzbService.GetNzb(id)` and return the result

#### Scenario: Caps returned directly
- **WHEN** a request with `?t=caps` arrives
- **THEN** the controller SHALL return a serialized `Caps` response without service delegation

### Requirement: Search pagination parameters
The system SHALL accept `offset` (int, default 0) and `limit` (int, default 100) query parameters on all search endpoints. Paging SHALL be applied exactly once in `NewznabSearchService` after retrieving cached results. The RSS mapper SHALL NOT apply additional paging.

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

## ADDED Requirements

### Requirement: NewznabCategory handles TV range
`NewznabCategory.FromCat` SHALL explicitly map TV category range (5000-5999) to the TV category, in addition to the existing Movie range (2000-2999).

#### Scenario: Movie category mapped
- **WHEN** `FromCat(2040)` is called
- **THEN** the result SHALL be `NewznabCategory.Movie`

#### Scenario: TV category mapped
- **WHEN** `FromCat(5040)` is called
- **THEN** the result SHALL be `NewznabCategory.Tv`

#### Scenario: Unknown category returns null
- **WHEN** `FromCat(9999)` is called
- **THEN** the result SHALL be `null`

#### Scenario: Null category returns null
- **WHEN** `FromCat(null)` is called
- **THEN** the result SHALL be `null`

### Requirement: SearchResultCache cleanup
`SearchResultCache.GetOrAddAsync` SHALL remove pending entries in a `finally` block only. There SHALL be no redundant removal in a `catch` block.

#### Scenario: Pending entry removed after success
- **WHEN** `GetOrAddAsync` completes successfully
- **THEN** the pending entry SHALL be removed from the `_pending` dictionary

#### Scenario: Pending entry removed after failure
- **WHEN** `GetOrAddAsync` throws an exception
- **THEN** the pending entry SHALL be removed from the `_pending` dictionary via the `finally` block
- **AND** the exception SHALL be rethrown
