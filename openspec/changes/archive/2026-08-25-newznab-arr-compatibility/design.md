## Context

FunkArr emulates the Newznab protocol to serve as an indexer for Sonarr, Radarr,
and Prowlarr. The current implementation handles the core search flows correctly
but omits several attributes and protocol elements that these tools actively use
for result matching and pagination.

The search pipeline already resolves rich metadata (ImdbId via TMDB, ReleaseYear,
TvdbId via TVDB) but this data is lost before reaching the Newznab XML response.
`SearchResult` lacks fields for ImdbId, Year, and ResolvedTvdbId — the mapper can
only emit what the controller passes from query parameters.

## Goals / Non-Goals

**Goals:**
- Emit `imdbid`, `year`, and `tvdbid` attributes from pipeline-resolved data
- Apply pagination consistently across all search types (TV, Movie, Text)
- Report correct `offset`/`total` in `<newznab:response>` for pagination clients
- Advertise `<limits>` in caps so Prowlarr knows the max result count

**Non-Goals:**
- Server-side category filtering (Sonarr/Radarr filter client-side)
- Moving pagination into actors (in-memory slicing is sufficient at current scale)
- `t=get` endpoint (enclosure URL works for all three clients)
- Genre attribute (low impact, separate change)

## Decisions

### D1: Enrich SearchResult rather than pass-through from controller

**Decision:** Add `ImdbId`, `Year`, and `ResolvedTvdbId` to `SearchResult` and
populate them in the pipeline.

**Alternative:** Pass query-param values through the controller to the mapper.
Rejected because query-based searches (e.g., `t=movie&q=Das Boot`) would never
get these attributes — the controller doesn't have them.

**Rationale:** The pipeline already resolves this data. Threading it into
`SearchResult` makes it available regardless of search origin.

### D2: In-memory pagination with accurate totals

**Decision:** All three handlers (TV, Movie, Text) apply `Skip(offset).Take(limit)`
after receiving full results from actors. `SerializeFeed` receives the pre-slice
total count and the requested offset.

**Alternative:** Push pagination into actors with count-aware queries. Rejected —
the Mediathek API doesn't support server-side pagination, and result sets are
small (typically < 250 items).

**Rationale:** Minimal change. The actors already return everything; slicing in
the controller is simple and correct.

### D3: limits max=100

**Decision:** Advertise `<limits max="100" default="100" />` in caps.

**Rationale:** Matches the existing default in TextSearch. The Mediathek API
returns at most ~250 results but many are filtered by ContentFilter. 100 is a
safe upper bound for what FunkArr realistically serves per query.

### D4: Pipeline threading approach

**Decision:** Populate new `SearchResult` fields in `SearchRequestActor` during
result assembly (the `BuildResults` / final mapping step), not in the individual
media actors.

**Rationale:** `SearchRequestActor` is the orchestrator that already builds
`SearchResult` from `MatchedItemInfo`. It has access to the resolved metadata
via `SearchHint` (which carries TvdbId from ShowActor) and `MatchContext`
(which carries ImdbId from MovieActor). Year comes from
`TmdbGatewayActor.MovieInfoResponse.ReleaseYear` and needs to be threaded
through `SearchHint` or `MatchContext` for movies.

## Risks / Trade-offs

- **[Snapshot churn]** → Caps and TV search verified snapshots need updating.
  Controlled risk — the snapshots exist precisely to catch unintended changes.
  Update them deliberately.

- **[Year availability]** → Year is only available for movie searches (via TMDB).
  TV show year could come from TVDB but is less useful for Sonarr matching.
  → Accept: emit year only when available (movies). TV year is a future
  enhancement if needed.

- **[ImdbId for query-based movie searches]** → When searching by query
  (`t=movie&q=Das Boot`), the MovieActor resolves via TMDB and may find an
  ImdbId. Threading this requires `SearchHint` or `MatchContext` to carry the
  resolved ImdbId back. → The `MatchContext` already has an `ImdbId` field —
  ensure it's populated and threaded to `SearchResult`.

- **[ResolvedTvdbId for query-based TV searches]** → ShowActor resolves by name
  and its entity key IS the tvdbid (when resolved). The `SearchHint` doesn't
  currently carry tvdbid explicitly, but the `SearchRequestActor` knows it from
  the entity key or the original request. → Thread from the request's TvdbId or
  resolve it from the ShowActor's entity key.
