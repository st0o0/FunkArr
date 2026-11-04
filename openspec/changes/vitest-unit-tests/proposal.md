## Why

UI logic — release title parsing, status computation, row mapping, phase stepper state, formatting — is inlined in Vue views where it can only be tested by rendering in a browser. The Playwright smoke tests catch crashes but not logic bugs. We tried detailed Playwright tests for every interaction and filter but they proved fragile (locator breakage on any text/class change) and mostly tested Vue's own reactivity rather than our logic. Extracting pure logic into utility functions and testing with Vitest gives fast, stable coverage of the code that actually has bugs.

## What Changes

- Add Vitest as dev dependency with Vue plugin
- Extract pure logic from Activity.vue → `utils/activityMapping.ts` (row mapping, status labels)
- Extract pure logic from DownloadDetail.vue → `utils/phaseState.ts` (stepper state, CSS classes)
- Export matching functions from `useRulesetMatcher.ts`
- Add ~100 unit tests across 8 test files covering all extracted logic + existing utilities
- Update GitHub Actions to run `pnpm test` alongside Playwright smoke tests

## Capabilities

### New Capabilities

- `vitest-unit-tests`: Vitest infrastructure, logic extraction from views into pure utilities, and unit test suite covering release title parsing, formatting, activity row mapping, phase stepper state, ruleset matching, enum maps, grouped queue, and toast behavior

### Modified Capabilities

_(none — extracting logic is a refactor with no behavior change)_

## Impact

- **Frontend**: New `vitest.config.ts`, new `utils/activityMapping.ts` and `utils/phaseState.ts`, new `__tests__/` directory with 8 test files
- **Views**: Activity.vue and DownloadDetail.vue import from new utility files instead of defining logic inline
- **CI**: `pnpm test` step added to GitHub Actions workflow
- **No backend changes**
