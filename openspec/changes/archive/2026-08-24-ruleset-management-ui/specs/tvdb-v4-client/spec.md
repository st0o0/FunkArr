## ADDED Requirements

### Requirement: TVDB v4 authentication handler
The system SHALL provide a `TvdbAuthHandler` (DelegatingHandler) that acquires a JWT bearer token via `POST /v4/login` with the configured API key, caches the token for 24 hours, and attaches `Authorization: Bearer {token}` to all outgoing requests.

#### Scenario: Token acquisition on first request
- **WHEN** the first HTTP request is made through the handler and no cached token exists
- **THEN** the handler SHALL call `POST /v4/login` with `{ "apikey": "{configured key}" }`, cache the returned token, and attach it as a Bearer header

#### Scenario: Token reuse within cache window
- **WHEN** a request is made within 24 hours of the last token acquisition
- **THEN** the handler SHALL reuse the cached token without calling the login endpoint

#### Scenario: Token refresh after cache expiry
- **WHEN** a request is made more than 24 hours after the last token acquisition
- **THEN** the handler SHALL acquire a new token before forwarding the request

#### Scenario: Login failure
- **WHEN** the login endpoint returns an error (invalid key, unreachable)
- **THEN** the handler SHALL forward the request without a token and log a warning

### Requirement: TVDB v4 base URL
The `TvdbClient` HttpClient SHALL use base URL `https://api4.thetvdb.com/v4`.

#### Scenario: HttpClient configuration
- **WHEN** the application starts and registers the TvdbClient HttpClient
- **THEN** the base address SHALL be `https://api4.thetvdb.com/v4`

### Requirement: TVDB v4 series endpoint
`GetShowAsync` SHALL call `GET /series/{tvdbId}` for basic series info and `GET /series/{tvdbId}/translations/deu` for the German name.

#### Scenario: Series lookup with German translation
- **WHEN** `GetShowAsync(329324)` is called
- **THEN** it SHALL fetch `/series/329324` for base info and `/series/329324/translations/deu` for the German series name

#### Scenario: No German translation available
- **WHEN** the German translation endpoint returns empty
- **THEN** it SHALL fall back to the base series `name` field

### Requirement: TVDB v4 episodes endpoint with German translations
`GetEpisodesAsync` SHALL call `GET /series/{tvdbId}/episodes/default/deu?season={season}&page=0` to get episodes with German names in a single request.

#### Scenario: Episode lookup with season filter
- **WHEN** `GetEpisodesAsync(329324, 1)` is called
- **THEN** it SHALL fetch `/series/329324/episodes/default/deu?season=1&page=0`

#### Scenario: Pagination handling
- **WHEN** the response indicates more pages exist via `links.next`
- **THEN** the client SHALL fetch subsequent pages and concatenate all episodes

### Requirement: TVDB v4 response model field mapping
The response models SHALL use the v4 field names: `name` (was episodeName/seriesName), `seasonNumber` (was airedSeason), `number` (was airedEpisodeNumber), `aired` (was firstAired).

#### Scenario: TvdbEpisodeInfo field mapping
- **WHEN** deserializing a v4 episode response
- **THEN** `EpisodeName` SHALL map from JSON field `name`, `AiredSeason` from `seasonNumber`, `AiredEpisodeNumber` from `number`, `FirstAired` from `aired`

### Requirement: TVDB v4 series search
The client SHALL provide a `SearchSeriesAsync(query)` method for searching shows by name via `GET /search?query={query}&type=series`.

#### Scenario: Search for a show
- **WHEN** `SearchSeriesAsync("Feuer & Flamme")` is called
- **THEN** it SHALL query `/search?query=Feuer+%26+Flamme&type=series` and return matching series with tvdbId, name, and year
