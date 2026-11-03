## 1. Backend — Extend QueryFeed message

- [x] 1.1 Add `Channel`, `Quality`, `Search` fields to `QueryFeed` message (Offset and MediaType already exist)
- [x] 1.2 Add `FilteredTotal` field to `FeedResult` response

## 2. Backend — FeedStoreManager filtering

- [x] 2.1 Update `FeedStoreManagerState.QueryAll` to apply filters: MediaType → Channel → Quality → Search → compute filteredTotal → Skip(offset) → Take(limit)
- [x] 2.2 Search filter: case-insensitive contains match on Title and Topic

## 3. Backend — Wire API query parameters

- [x] 3.1 Expose `offset` query parameter in `FeedEndpoints.cs` (pass to QueryFeed.Offset)
- [x] 3.2 Expose `type` query parameter (parse to MediaType enum, pass to QueryFeed.MediaType)
- [x] 3.3 Expose `channel`, `quality`, `q` query parameters (pass to new QueryFeed fields)
- [x] 3.4 Include `filteredTotal` in `FeedOverviewResponse` JSON output

## 4. Frontend — Stat cards

- [x] 4.1 Replace definition-list ingest section with four stat cards: Total Entries, Last Crawl (+N new + relative time), Unmatched, Status
- [x] 4.2 Status card: show crawl outcome, consecutiveFailures if > 0, gap warning if lastGap true
- [x] 4.3 Keep "Crawl now" button in page header, keep hint text about RSS sync at bottom

## 5. Frontend — Filter bar

- [x] 5.1 Add filter bar below stat cards: search input, channel dropdown, type dropdown, quality dropdown
- [x] 5.2 Channel dropdown populated from distinct channels in entry data
- [x] 5.3 Type dropdown: All / Show / Movie
- [x] 5.4 Quality dropdown: All / 480p / 720p / 1080p
- [x] 5.5 Search input with 300ms debounce
- [x] 5.6 All filter changes trigger API re-fetch and reset to page 1

## 6. Frontend — Paginated table

- [x] 6.1 Replace flat entry list with table: Time (relative), Title (with S##E## badge), Channel, Quality, Type
- [x] 6.2 Add pagination controls: Back/Next buttons + page indicator ("Seite X / Y")
- [x] 6.3 Add page size selector (25/50/100) persisted in localStorage
- [x] 6.4 Pagination uses offset = (page - 1) * pageSize, total from filteredTotal

## 7. Frontend — API client + composable

- [x] 7.1 Extend `api/feed.ts` with new query params (offset, type, channel, quality, q)
- [x] 7.2 Create `useFeed` composable managing filter state, pagination state, and API calls

## 8. i18n

- [x] 8.1 Add/update i18n keys for stat cards, filter labels, pagination controls, page size labels in all 4 locales (en, de, de-AT, de-CH)

## 9. Tests

- [x] 9.1 Unit test: FeedStoreManagerState.QueryAll with channel/quality/search filters
- [x] 9.2 Unit test: FeedStoreManagerState.QueryAll returns correct filteredTotal with combined filters
- [x] 9.3 Unit test: FeedStoreManagerState.QueryAll offset beyond range returns empty entries with correct totals
