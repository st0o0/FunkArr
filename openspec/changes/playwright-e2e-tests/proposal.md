## Why

The UI has no automated browser tests. Unit tests cover backend logic (xUnit per domain), integration tests cover API endpoints (TestServer-based), and architecture tests enforce boundaries — but nothing verifies that Vue components render correctly, interactions work, or edge cases are handled in the browser. The existing `e2e-verify` skill uses `claude-in-chrome` for ad-hoc verification but is not CI-capable. Every UI change ships without automated regression coverage.

## What Changes

- Add Playwright and MSW (Mock Service Worker) as dev dependencies to FunkArr.UI
- Create a comprehensive E2E test suite (~150-180 test cases across 12 spec files) covering all 12 views
- Build a typed fixture system with mock data for all ~40 API endpoints and 2 SSE streams
- Create SSE mock helpers for testing live queue updates and download transitions
- Configure Playwright to auto-start Vite dev server and run in CI (GitHub Actions)
- Tests run against Vite dev server with MSW intercepting all API calls — no Docker needed, fully deterministic

## Capabilities

### New Capabilities

- `playwright-test-infrastructure`: Playwright + MSW setup, fixture system, SSE mock helpers, Vite dev server integration, CI configuration
- `playwright-smoke-tests`: Route loading, redirect verification, JS error detection across all views
- `playwright-layout-tests`: Sidebar navigation, collapse/expand, locale switching, active route highlighting
- `playwright-activity-tests`: Activity table rendering, filter chips, search, dropdowns, pagination, inline actions, pipeline controls, empty states
- `playwright-download-detail-tests`: Active/Completed/Failed states, phase stepper, live transition (SSE → history fetch → toast), direct URL navigation, error handling
- `playwright-ruleset-tests`: List search/filter/sort, detail sections, export/delete, builder form validation and save, scoring history pagination and traces
- `playwright-remaining-view-tests`: Feed page, Settings page, Setup wizard — filter/search/pagination, config display, multi-step wizard flow

### Modified Capabilities

_(none — this adds tests, no existing behavior changes)_

## Impact

- **Frontend**: New `e2e/` directory under FunkArr.UI with test files, fixtures, and config
- **Dependencies**: `@playwright/test` and `msw` added as devDependencies
- **CI**: New GitHub Actions step for Playwright (browser install + test run)
- **No backend changes**: All API interactions are mocked via MSW
