## MODIFIED Requirements

### Requirement: TVDB v4 authentication handler
The system SHALL provide a `TvdbAuthHandler` (DelegatingHandler) that acquires a JWT bearer token via `POST /v4/login` with the configured API key, caches the token for 24 hours, and attaches `Authorization: Bearer {token}` to all outgoing requests. The handler SHALL be registered with `TvdbProvider` (renamed from `TvdbV4Client`).

#### Scenario: Token acquisition on first request
- **WHEN** the first HTTP request is made through the handler and no cached token exists
- **THEN** the handler SHALL call `POST /v4/login` with `{ "apikey": "{configured key}" }`, cache the returned token, and attach it as a Bearer header

#### Scenario: Token reuse within cache window
- **WHEN** a request is made within 24 hours of the last token acquisition
- **THEN** the handler SHALL reuse the cached token without calling the login endpoint

## RENAMED Requirements

### Requirement: TvdbV4Client renamed to TvdbProvider
- **FROM:** `TvdbV4Client` in `Search/Resolvers/`
- **TO:** `TvdbProvider` in `Providers/Tvdb/`

## ADDED Requirements

### Requirement: TvdbProvider returns IQueryResult
All public methods on `TvdbProvider` SHALL return `Task<IQueryResult>` instead of throwing on failure. The provider SHALL have one catch site translating exceptions to `QueryFailure`.

#### Scenario: Successful series lookup
- **WHEN** the TVDB API returns series data
- **THEN** `TvdbProvider` SHALL return `QuerySuccess<TvdbSeriesInfo>(data)`

#### Scenario: Series not found
- **WHEN** the TVDB API returns 404
- **THEN** `TvdbProvider` SHALL return `QueryFailure(NotFound, "Series {id} not found")`

#### Scenario: Auth failure
- **WHEN** the TVDB API returns 401
- **THEN** `TvdbProvider` SHALL return `QueryFailure(Unauthorized, ...)`

#### Scenario: Network error
- **WHEN** an HttpRequestException occurs
- **THEN** `TvdbProvider` SHALL return `QueryFailure(Transport, ex.Message)`
