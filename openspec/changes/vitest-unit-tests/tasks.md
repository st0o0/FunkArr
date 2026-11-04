## 1. Infrastructure

- [x] 1.1 Add `vitest` as devDependency, create `vitest.config.ts` with `@vitejs/plugin-vue`
- [x] 1.2 Add `pnpm test` script to package.json
- [x] 1.3 Update `.github/workflows/ui-tests.yml` to run `pnpm test` before Playwright

## 2. Extract Logic from Views

- [x] 2.1 Extract `queueItemToRow()`, `historyItemToRow()`, `priorityLabel()` from Activity.vue → `utils/activityMapping.ts`
- [x] 2.2 Update Activity.vue to import from `utils/activityMapping.ts`
- [x] 2.3 Extract `isStepDone()`, `isStepFailed()`, `isStepCurrent()`, `stepClass()`, `stepLabelClass()` from DownloadDetail.vue → `utils/phaseState.ts`
- [x] 2.4 Update DownloadDetail.vue to import from `utils/phaseState.ts`
- [x] 2.5 Export `matchCandidate()`, `tryMatch()`, and strategy functions from `useRulesetMatcher.ts`

## 3. Tier 1 Tests — Pure Functions

- [x] 3.1 Write `__tests__/releaseTitle.test.ts` — SxxExx, date, Folge, multi-episode, quality, no-quality, empty, malformed
- [x] 3.2 Write `__tests__/format.test.ts` — formatSize (0, KB, MB, GB boundaries), formatSpeed, formatDuration (0, large), formatPercent
- [x] 3.3 Write `__tests__/enumMaps.test.ts` — all 10 name-lookup functions, unknown value fallback

## 4. Tier 2 Tests — Extracted View Logic

- [x] 4.1 Write `__tests__/activityMapping.test.ts` — active/queued/completed/failed row mapping, status labels, dots, priority labels, position, size formatting
- [x] 4.2 Write `__tests__/phaseState.test.ts` — active at each phase, completed (all done), failed (failure indicator), CSS class verification

## 5. Tier 3 Tests — Composables

- [x] 5.1 Write `__tests__/rulesetMatcher.test.ts` — SeasonEpisode, AbsoluteEpisode, TitleExact, TitleIncludes, Airdate strategies, invalid regex safety, edge cases
- [x] 5.2 Write `__tests__/groupedQueue.test.ts` — grouping by series, active-first sort, empty queue, single item
- [x] 5.3 Write `__tests__/toast.test.ts` — add toast, max-3 cap, auto-remove with fake timers, variant types

## 6. Verify

- [x] 6.1 Run full Vitest suite green (92 tests, 271ms)
- [x] 6.2 Run Playwright smoke tests still green after view refactoring (13 tests)
- [x] 6.3 Run `pnpm run build` to verify no TypeScript errors
