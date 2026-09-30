## Why

Sonarr, Radarr, and Prowlarr rely on specific Newznab protocol features to
confidently match and import results. FunkArr's current Newznab emulation is
missing several attributes and protocol elements that these tools actively use,
causing reduced matching confidence, incomplete pagination, and warnings in
Prowlarr's indexer UI.

## What Changes

**Group A — Controller/Serializer (no actor changes):**
- Apply `limit`/`offset` pagination to TV and Movie search handlers (currently only TextSearch paginates)
- Add `<limits max="100" default="100" />` element to caps XML response
- Fix `SerializeFeed` to emit correct `offset` (from request) and `total` (full result count, not page count)

**Group B — SearchResult enrichment (pipeline threading):**
- Add `ImdbId` to `SearchResult`, threaded from MovieActor/TmdbGateway pipeline; emit as `newznab:attr name="imdbid"` in movie responses
- Add `Year` to `SearchResult`, threaded from `TmdbGatewayActor.MovieInfoResponse.ReleaseYear`; emit as `newznab:attr name="year"` in movie responses
- Add `ResolvedTvdbId` to `SearchResult`, threaded from ShowActor resolution; emit `tvdbid` attribute even on query-based TV searches (not just when the caller sent a tvdbid)

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `newznab-indexer`: Add pagination to TV/movie handlers, correct offset/total in response, emit imdbid/year/tvdbid attributes from enriched SearchResult
- `newznab-xml-models`: Add `<limits>` element to caps serialization
- `search-request-actor`: Thread ImdbId, Year, and ResolvedTvdbId through pipeline into SearchResult
- `contract-tests`: Update verified snapshots for caps (limits element) and TV search (tvdbid attribute on resolved results)

## Impact

- **Controller**: `NewznabController` — pagination + offset/total threading in all three search handlers
- **Mapper**: `NewznabResultMapper` — emit imdbid, year, tvdbid from SearchResult fields instead of controller params
- **Serializer**: `NewznabSerializer.SerializeCaps` — add limits element
- **Model**: `SearchResult` — three new nullable properties (ImdbId, Year, ResolvedTvdbId)
- **Pipeline**: `SearchRequestActor` result assembly — populate new SearchResult fields from resolved data
- **Tests**: Contract snapshots need updating; mapper tests need new attribute assertions
- **No breaking changes** — all additions are backward-compatible new attributes and protocol elements
