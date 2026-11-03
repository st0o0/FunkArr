## Context

The /feed page currently has two sections: a definition-list showing last crawl stats, and a flat entry list. The API (`GET /api/feed?limit=N`) already supports `offset` and `mediaType` filtering at the message/store level but doesn't expose them as query parameters. The FeedStoreManager holds entries in an `ImmutableSortedSet<FeedEntry>` sorted by `FirstSeenAt` descending — max ~2000 entries, well suited for in-memory filtering.

## Goals / Non-Goals

**Goals:**
- Clear visual separation between all-time stats and per-crawl stats via stat cards
- Paginated table with configurable page size (25/50/100)
- Filter bar: text search, channel, type (Show/Movie), quality
- All filtering server-side via API query parameters
- Consistent with existing UI patterns (Dashboard stat cards, Rulesets filter bar)

**Non-Goals:**
- Column sorting (entries are always sorted by firstSeenAt descending)
- Entry detail view or click-through
- Real-time entry updates via SSE (manual refresh or re-fetch on Crawl Now)
- Infinite scroll (explicit pagination preferred)

## Decisions

### 1. Server-side filtering over client-side

All filters go through the API as query parameters. The FeedStoreManager applies them as LINQ predicates before pagination. This keeps the UI simple (no local filter state on 2000+ entries) and ensures `total` reflects the filtered count for correct pagination.

Alternative: Client-side filtering on the full 500-entry response. Rejected because it limits the browsable set to 500 and makes pagination math inconsistent.

### 2. Extend QueryFeed message with optional filter fields

```
QueryFeed(int Limit, int Offset, MediaType? MediaType,
          string? Channel, int? Quality, string? Search)
```

FeedStoreManagerState.QueryAll applies filters in order: MediaType → Channel → Quality → Search → Skip(offset) → Take(limit). Each filter is a simple LINQ `.Where()` on the sorted set. Search matches against Title and Topic (case-insensitive contains).

The response includes `filteredTotal` alongside `total` so the UI can show "Showing 25 of 342 (1842 total)".

### 3. Stat cards layout matching Dashboard

Four cards in a row:
- **Entries**: `total` (all-time) — links nowhere
- **Last Crawl**: `+lastNewItems new` with `lastRunAt` relative time — shows delta
- **Unmatched**: `lastUnmatched` count — entries without matching ruleset
- **Status**: crawl status icon + `lastOutcome` label, `consecutiveFailures` if > 0

Below the cards: a single status line with cursor timestamp. This replaces the current definition-list.

### 4. Page size selector as segmented control

Three options: 25 / 50 / 100. Default 25. Stored in localStorage. Changing page size resets to page 1. The API `limit` parameter maps directly to page size.

### 5. Channel dropdown populated from entry data

The channel filter dropdown is populated from distinct channels in the current entry set. The API already returns channel per entry — the UI collects unique values from the full response to build the dropdown. No separate endpoint needed since the channel set is small (~15 broadcasters).

Alternative: Dedicated `/api/feed/channels` endpoint. Rejected — unnecessary for ~15 values that are already in the entry data.

### 6. FeedOverviewResponse shape change

Current response: `{ entries[], total, ingest: {...} }`.
New response: `{ entries[], total, filteredTotal, ingest: {...} }`.

`total` stays as all-time count (for stat card). `filteredTotal` is the count after applying filters (for pagination). When no filters active, `filteredTotal == total`.

## Risks / Trade-offs

- **In-memory filtering on 2000 entries**: Acceptable performance. LINQ Where + Skip + Take on ImmutableSortedSet is O(n) but n ≤ 2000. If feed grows significantly, consider indexed lookups.
- **Channel dropdown stale after filter**: If user filters by Type=Movie, the channel dropdown still shows all channels (not just movie channels). This is intentional — cross-filtering would require a more complex query model. Keeping it simple.
- **No debounce on search**: Text search triggers an API call on each keystroke. Add 300ms debounce in the UI composable to avoid excessive requests.
