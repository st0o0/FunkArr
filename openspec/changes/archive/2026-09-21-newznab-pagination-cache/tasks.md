# Tasks: Newznab Pagination Cache

## Phase 1: Fix ToRss pagination (quick win)

- [x] **1.1** Add `ToRss` unit tests for offset/limit behavior
  - `ToRss_Offset0_ReturnsFirstItems`
  - `ToRss_Offset100_SkipsFirstItems`
  - `ToRss_OffsetBeyondItems_ReturnsEmpty`
  - `ToRss_TotalIsFullCount_NotPageCount`
- [x] **1.2** Fix `SearchHandler.ToRss` to use `.Skip(offset).Take(limit)` instead of `.Take(limit)`

## Phase 2: SearchResultCache

- [x] **2.1** Create `SearchResultCache` with TDD
  - Write tests first: key generation, set/get, TTL expiry, concurrent coalescing
  - Implement `SearchResultCache` class with `ConcurrentDictionary` + `Lazy<Task>` pattern
- [x] **2.2** Wire `SearchResultCache` into DI as singleton
  - Register in `ServiceCollectionExtensions` or equivalent
  - Add `NewznabOptions.CacheTtlSeconds` (default: 60)

## Phase 3: Integrate cache into SearchHandler

- [x] **3.1** Add `SearchHandler` integration tests with mocked cache
  - Cache miss → calls SearchManager, stores result
  - Cache hit → no SearchManager call, returns paged slice
  - Concurrent requests → single actor call
- [x] **3.2** Refactor `SearchHandler` to accept `SearchResultCache` and use `GetOrAddAsync`
  - On cache miss: send `SearchCommand` with `Offset = null` to fetch full result
  - Cache the `SearchResultItem[]` from the response
  - Paginate with `.Skip(offset).Take(limit)` on cached items
- [x] **3.3** Update `NewznabApiEndpoints` to resolve and pass cache to `SearchHandler`

## Phase 4: Verify

- [x] **4.1** Run full test suite
- [x] **4.2** Manual verification: trigger Sonarr search, confirm 1 history entry instead of 40
