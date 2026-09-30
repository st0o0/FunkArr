## 1. API - Extend mediathek search endpoint

- [x] 1.1 Add `MediathekSearchResponse` record wrapping items array with `TotalResults` field to replace raw array return
- [x] 1.2 Extend `MediathekSearchResult` with `Size` (long), `Description` (string?), `HasSubtitles` (bool), `HasHd` (bool), `WebsiteUrl` (string?)
- [x] 1.3 Add query parameters to the endpoint: `channel`, `topic`, `durationMin`, `durationMax`, `offset`, `sortBy`, `sortOrder`
- [x] 1.4 Update `QueryMediathek` construction in the endpoint to use new parameters: add channel/topic as additional `MediathekQueryField` entries, pass duration filters and sort/pagination params
- [x] 1.5 Update result mapping to populate new fields from `MediathekItem` (Size, Description, UrlSubtitle != null, UrlVideoHd != null, UrlWebsite)

## 2. Frontend API layer

- [x] 2.1 Add `searchMediathek(params)` function to a new `api/mediathek.ts` with typed request params and response model
- [x] 2.2 Define TypeScript types for `MediathekSearchResponse`, `MediathekSearchItem`

## 3. Frontend - Search view

- [x] 3.1 Create `views/Search.vue` with search input, filter controls (channel, topic, duration range), results grid, pagination, loading/empty/error states
- [x] 3.2 Implement 400ms debounced search with URL query param sync (q, channel, topic, durationMin, durationMax, page)
- [x] 3.3 Implement result cards: title heading, topic/channel metadata, quality badge, duration, aired date, size, description (truncated with expand), subtitle/HD indicators
- [x] 3.4 Implement offset-based pagination controls with "Page N of M" display and prev/next buttons

## 4. Frontend - Navigation integration

- [x] 4.1 Add `/search` route to router
- [x] 4.2 Add "Search" entry to sidebar navigation in the "Media" group between Dashboard and Downloads

## 5. API endpoint tests

- [x] 5.1 Add tests for extended mediathek search endpoint in `FunkArr.Api.Tests` - verify new query params map to `QueryMediathek` correctly, verify enriched response model
