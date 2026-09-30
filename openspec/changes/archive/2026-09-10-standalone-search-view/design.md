## Context

The MediathekViewWeb API supports full-text search with field targeting, duration filters, sorting, and pagination (`offset`/`size`). The `MediathekViewWebManager` already handles all of this, and `QueryMediathek` exposes the full parameter set. However, the internal API endpoint (`GET /api/mediathek/search`) only accepts `q` and `limit`, constructs a minimal query targeting `title` and `topic` fields, and returns a stripped-down `MediathekSearchResult` that drops size, URL variants, website link, and the total result count.

The DebuggerPanel in the RuleSet Builder has a search feature but it converts results into test candidates for scoring - it's not a browsing interface.

## Goals / Non-Goals

**Goals:**
- Extend the existing mediathek search endpoint with richer response and more query parameters
- Build a standalone `/search` view for browsing Mediathek content
- Support pagination and display total result counts
- Show rich metadata per result: title, topic, channel, duration, quality, aired date, size, description

**Non-Goals:**
- Adding download-from-search functionality (that flows through Sonarr/Radarr)
- Advanced faceted search (channel picklist populated from API) - keep filters simple text inputs
- Caching or saving search results
- Integrating with the scoring/matching pipeline

## Decisions

### 1. Extend existing endpoint vs. new endpoint

**Decision**: Extend the existing `GET /api/mediathek/search` endpoint with additional query parameters and a richer response model.

The current endpoint already does the right thing - it talks to MediathekViewWebManager and maps results. Adding parameters (`offset`, `channel`, `topic`, `durationMin`, `durationMax`, `sortBy`, `sortOrder`) and enriching the response is simpler than maintaining two endpoints for the same actor query. All new response fields are additive.

**Alternative considered**: New `GET /api/mediathek/browse` endpoint. Rejected - the underlying query is identical, and having two endpoints querying the same actor with the same message is unnecessary duplication.

### 2. Response model: flat enrichment

**Decision**: Extend `MediathekSearchResult` with: `Size` (long), `Description` (string?), `HasSubtitles` (bool), `HasHd` (bool), `WebsiteUrl` (string?). Wrap the array in an object with `TotalResults` (int) from `MediathekQueryCompleted.Total`.

Map quality variants to boolean flags (`HasHd`, `HasSubtitles`) rather than exposing raw URLs - the UI doesn't need download URLs, just indicators. The `Quality` field already provides the estimated resolution.

**Alternative considered**: Expose all URL variants. Rejected - raw video URLs aren't useful in a browse view and would be confusing. The existing `EstimateQuality` helper already derives the meaningful quality number.

### 3. Pagination: offset-based with page controls

**Decision**: Use offset-based pagination matching the MediathekViewWeb API's native `offset`/`size` parameters. The API already supports this. The UI sends `offset` and `limit` query params, the response includes `totalResults` for the UI to compute page count.

The UI will show "Page N of M" controls and a configurable page size (default 20). No infinite scroll - the total count enables proper pagination UX.

### 4. Search debouncing

**Decision**: 400ms debounce on the search input. Typing triggers a debounced search after the user pauses. The search button also works for explicit triggering. URL query params sync with search state for deep linking.

### 5. UI layout

**Decision**: Card-based results in a responsive grid (1-2 columns). Each card shows: title (heading), topic + channel (metadata line), quality badge, duration formatted, aired date, size formatted, description (truncated, expandable). Filter inputs above the grid: search query (full width), channel and topic (optional text inputs), duration range.

## Risks / Trade-offs

- **MediathekViewWeb rate limiting** - The API has backpressure (3 concurrent requests via stashing), but frequent user searches could still hit upstream limits. The 400ms debounce mitigates this. No additional throttling needed at the API level since the actor already handles it.
- **Total result count accuracy** - MediathekViewWeb's `totalResults` may be approximate for large result sets. Display it as informational, not exact.
- **Response size** - Adding `Description` to each result increases payload size. Descriptions are typically short (1-2 sentences), so this is acceptable for 20-item pages.
