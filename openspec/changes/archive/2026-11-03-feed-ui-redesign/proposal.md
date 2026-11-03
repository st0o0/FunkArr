## Why

The /feed page displays a flat dump of published entries with confusing metrics: "Veröffentlicht: 12" (last crawl) sits next to "Veröffentlichte Releases (1842)" (all-time total) — same term, different scopes. There is no pagination, no filtering by channel/type/quality, no text search, and no page size control. Users cannot meaningfully browse or find entries in the feed store.

## What Changes

- Replace the definition-list ingest section with **stat cards** (Total Entries, Last Crawl delta, Unmatched, Gaps) matching the Dashboard pattern
- Add a **filter bar** with text search, channel dropdown, type dropdown (Show/Movie), and quality dropdown
- Replace the flat entry list with a **paginated table** with sortable columns
- Add a **page size selector** (25/50/100) and Back/Next pagination controls
- **Wire existing backend capabilities**: `offset` and `mediaType` filter already exist in QueryFeed but are not exposed as API query parameters
- Add new backend filters: `channel`, `quality`, `q` (text search) as in-memory LINQ filters in FeedStoreManager
- Return filtered `total` count for correct pagination math

## Capabilities

### New Capabilities
- `feed-ui-layout`: Stat cards, filter bar, paginated table layout for the /feed page
- `feed-query-filters`: API query parameters for offset, type, channel, quality, and text search

### Modified Capabilities
- `feed-domain-model`: No requirement changes — the entry data model stays the same, only the query interface expands

## Impact

- `src/FunkArr.Api/FeedEndpoints.cs` — add query parameters (offset, type, channel, quality, q)
- `src/FunkArr.Messages/Feed/QueryFeed.cs` — add Channel, Quality, Search fields
- `src/FunkArr.Feed/FeedStoreManagerState.cs` — add in-memory filtering to QueryAll
- `src/FunkArr.UI/src/views/FeedView.vue` — full rewrite of the feed page component
- `src/FunkArr.UI/src/api/feed.ts` — extend API client with new query params
- `src/FunkArr.UI/src/locales/*.json` — new/updated i18n keys for stat cards, filters, pagination
