# Tasks: UI Scale & Polish

## Phase 1: Readable Titles

- [x] Create `parseReleaseName()` utility in `src/FunkArr.UI/src/utils/releaseTitle.ts`
  - Parse series name, SxxExx, episode title, quality from release name
  - Handle edge cases: no episode title, pipe separators, Audiodeskription suffix
  - Unit tests for common patterns

- [x] Create `<ReleaseTitle>` component in `src/FunkArr.UI/src/components/ReleaseTitle.vue`
  - Structured two-line display: series name + season/episode/title/quality
  - Tooltip with raw release name on hover
  - Compact variant for Dashboard widget (single line)

- [x] Integrate `<ReleaseTitle>` into `QueueCard.vue`
- [x] Integrate `<ReleaseTitle>` into `ActiveDownloads.vue`
- [x] Integrate `<ReleaseTitle>` into `History.vue` title column

## Phase 2: Queue Grouping

- [x] Create grouping composable `useGroupedQueue()` from `useQueueStream()`
  - Group items by parsed series name
  - Sort: groups with active downloads first, then alphabetical
  - Expose group metadata (active count, queued count, total size)

- [x] Create `<QueueGroupCard>` component
  - Collapsible group header with series name and counts
  - Expanded by default if group has active downloads
  - Collapsed by default for all-queued groups (show first 3 + "+N more")

- [x] Update `Queue.vue` to use grouped layout
  - Replace flat item list with grouped cards
  - Add "Expand All / Collapse All" toggle

## Phase 3: Dashboard Polish

- [x] Add stats row to `Home.vue`
  - Queue size, active count, total speed — from SSE stream data
  - Remove orphaned "View RuleSets" button

- [x] Improve `ActiveDownloads.vue` overflow
  - Show "+N more downloading" when >3 active
  - Total speed in widget header

## Phase 4: Small Fixes

- [x] Sort RuleSets alphabetically by topic in `RuleSetList.vue`
- [x] Show ruleset count ("61 rulesets") in `RuleSetList.vue`
- [x] Dynamic category list in `History.vue` filter
- [x] Add matched/unmatched filter toggle in `ScoringDetail.vue`
