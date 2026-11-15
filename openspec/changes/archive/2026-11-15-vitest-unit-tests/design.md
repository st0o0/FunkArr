## Context

FunkArr's Vue.js UI has pure business logic scattered across views: release title parsing with 5 regex paths, status/label mapping for 4 download states, phase stepper state computation, and a ruleset matching engine with 5 strategies. None of this needs DOM or Vue reactivity to test — it's data-in, data-out. Currently it's only testable by rendering the full page in a browser.

Existing test layers: xUnit unit tests (backend), ASP.NET TestServer integration tests (API), Playwright smoke tests (routes load). No frontend unit tests exist.

## Goals / Non-Goals

**Goals:**

- Sub-second test suite for all frontend business logic
- Extract logic from views so views become thin wiring
- Cover edge cases in parsing, formatting, and matching that would be expensive to test through the browser

**Non-Goals:**

- Component rendering tests (`mount()`, `shallowMount()`) — overkill, views are thin after extraction
- Testing Vue reactivity, router, or i18n — that's Vue's job
- Testing API fetch functions or EventSource wrappers — thin plumbing
- Replacing Playwright smoke tests — those stay for crash detection

## Decisions

### 1. Vitest over Jest

Vitest shares Vite's config and module resolution. Since the project already uses Vite, Vitest needs zero extra bundler configuration — just `@vitejs/plugin-vue` for `.vue` imports in Tier 3 tests. Jest would need its own transform pipeline.

### 2. Extract-then-test over test-in-place

Extract logic into `utils/*.ts` files, then test those files. The alternative — importing from `<script setup>` blocks — is fragile and couples tests to Vue's SFC compilation. After extraction, views become thin glue: import utilities, pass data, bind to template.

### 3. Three tiers of tests

**Tier 1 — Pure functions (no imports from Vue):** `parseReleaseName`, `formatSize`, `enumMaps`. Zero dependencies, instant execution.

**Tier 2 — Extracted view logic (no imports from Vue):** `queueItemToRow`, `historyItemToRow`, `isStepDone`, `stepClass`. Need the API type imports but no Vue reactivity.

**Tier 3 — Composables (need Vue refs/computed):** `useGroupedQueue`, `useToast`. Require `vi.mock` for `useQueueStream` and `vi.useFakeTimers` for timer logic. These import `vue` but not `@vue/test-utils`.

### 4. Test file location: `src/__tests__/`

Tests live alongside the code in `src/__tests__/*.test.ts`. This follows Vitest's default discovery pattern and keeps tests close to the code they test.

### 5. No `@vue/test-utils` dependency

None of the tests need to mount Vue components. Tier 3 tests use Vue's `ref()` and `computed()` directly. This avoids a dependency and the complexity of component mounting.

## Risks / Trade-offs

**[Refactoring views]** Extracting functions from Activity.vue and DownloadDetail.vue changes the view files. → Mitigation: The extraction is mechanical (move function, add export, add import). Playwright smoke tests verify the pages still load after refactoring.

**[Matching engine complexity]** `useRulesetMatcher.ts` has module-private functions that need to be exported for testing. → Mitigation: Export them as named exports. They're pure functions — making them public doesn't change behavior.
