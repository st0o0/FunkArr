## Purpose

TVDB v4 API client with authentication, episode data fetching, and response caching.

## Requirements

### Requirement: TvdbOptions configuration
The system SHALL define a `TvdbOptions` class bound to the `FunkArr:Tvdb` configuration section. It SHALL expose `ApiKey` (string, nullable) for the TVDB v4 API key. When `ApiKey` is null or empty, the TVDB client SHALL be disabled and all lookups SHALL return empty results. The class SHALL reside in `FunkArr.Core` namespace.

#### Scenario: API key configured
- **WHEN** `FunkArr__Tvdb__ApiKey` environment variable is set to a valid key
- **THEN** the TvdbOptions.ApiKey property SHALL contain that key

#### Scenario: No API key configured
- **WHEN** no TVDB API key is configured
- **THEN** TvdbOptions.ApiKey SHALL be null and the TVDB client SHALL skip all API calls

### Requirement: TVDB v4 API authentication
The TVDB client SHALL authenticate with the TVDB v4 API at `https://api4.thetvdb.com/v4` using Bearer token authentication. It SHALL POST to `/login` with the API key to obtain a token, and include the token as `Authorization: Bearer {token}` on subsequent requests.

#### Scenario: Successful authentication
- **WHEN** the TVDB client makes its first API call with a valid API key
- **THEN** it SHALL POST `{"apikey": "{apiKey}"}` to `/login` and store the returned token

#### Scenario: Token reuse
- **WHEN** the TVDB client has a valid cached token
- **THEN** subsequent requests SHALL reuse the token without re-authenticating

#### Scenario: Token refresh on 401
- **WHEN** a TVDB API request returns 401 Unauthorized
- **THEN** the client SHALL re-authenticate by posting to `/login` and retry the original request once

#### Scenario: Invalid API key
- **WHEN** the `/login` request fails with 401
- **THEN** the client SHALL log a warning and return an empty result without retrying

### Requirement: Fetch episodes by series ID
The TVDB client SHALL expose a method to fetch all episodes for a given TVDB series ID. It SHALL request `GET /series/{id}/episodes/default` with pagination support. The response SHALL be mapped to a list of `TvdbEpisode` records containing: SeasonNumber (int), EpisodeNumber (int), Name (string?), Aired (string?, ISO date), Runtime (int?).

#### Scenario: Fetch episodes for a series
- **WHEN** `FetchEpisodes(tvdbId: 83214)` is called
- **THEN** the client SHALL request `/series/83214/episodes/default` and return all episodes across all pages

#### Scenario: Paginated response
- **WHEN** a series has more episodes than one API page returns
- **THEN** the client SHALL follow the `links.next` URL to fetch subsequent pages and combine all episodes

#### Scenario: Series not found
- **WHEN** the TVDB API returns 404 for a series ID
- **THEN** the client SHALL return an empty episode list

#### Scenario: API error
- **WHEN** the TVDB API returns a 5xx error
- **THEN** the client SHALL log the error and return an empty episode list

### Requirement: Episode data caching
The TVDB client SHALL cache episode data per series ID. Cache entries SHALL use content-aware TTLs: active shows (with upcoming or recent episodes) SHALL use a 2-day TTL, inactive shows SHALL use a 7-day TTL. The default TTL of 12 hours SHALL remain as fallback. Cache SHALL be stored in-memory.

#### Scenario: Active show cache
- **WHEN** episodes for a series with a future episode airdate are fetched
- **THEN** they SHALL be cached with a 2-day TTL

#### Scenario: Inactive show cache
- **WHEN** episodes for a series with no recent or future airdates are fetched
- **THEN** they SHALL be cached with a 7-day TTL

#### Scenario: Cache hit within TTL
- **WHEN** episodes for series 83214 were fetched within the TTL
- **THEN** a subsequent request for series 83214 SHALL return cached data without an API call

#### Scenario: Cache miss after TTL
- **WHEN** a cache entry has expired
- **THEN** a subsequent request SHALL fetch fresh data from the TVDB API

#### Scenario: Cache for different series
- **WHEN** episodes for series 83214 are cached and series 390284 is requested
- **THEN** a fresh API call SHALL be made for series 390284

### Requirement: Filter episodes by season
The TVDB client SHALL support filtering cached episodes by season number. When a season is requested, only episodes matching that season SHALL be returned.

#### Scenario: Filter by season
- **WHEN** episodes for series 83214 are fetched and filtered by season 2026
- **THEN** only episodes with SeasonNumber=2026 SHALL be returned

#### Scenario: No season filter
- **WHEN** episodes are fetched without a season filter
- **THEN** all episodes across all seasons SHALL be returned

### Requirement: TvdbClient namespace
The TvdbClient class and TvdbEpisode record SHALL reside in the `FunkArr.MetadataResolver` namespace (renamed from `FunkArr.EpisodeGuide`).

#### Scenario: Namespace
- **WHEN** TvdbClient is referenced
- **THEN** it SHALL be in the `FunkArr.MetadataResolver` namespace
