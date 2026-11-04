# playwright-test-infrastructure Specification

## Purpose

Playwright and MSW test infrastructure: dependencies, config, fixture factories, SSE mock helpers, default API handlers, and CI integration.

## Requirements

### Requirement: Playwright configuration

The project SHALL have a `playwright.config.ts` that auto-starts the Vite dev server, runs tests from `e2e/specs/`, and configures Chromium as the default browser.

#### Scenario: Dev server auto-start
- **WHEN** Playwright tests are executed
- **THEN** Vite dev server SHALL start automatically on port 5173

#### Scenario: Existing server reuse
- **WHEN** Vite dev server is already running AND the CI environment variable is not set
- **THEN** Playwright SHALL reuse the existing server

### Requirement: MSW handler setup

The test infrastructure SHALL provide a default set of MSW handlers returning happy-path data for all API endpoints used by the UI.

#### Scenario: Default handlers cover all endpoints
- **WHEN** a test runs without custom handler overrides
- **THEN** all API calls from the UI SHALL be intercepted by MSW and return valid typed responses

#### Scenario: Per-test handler override
- **WHEN** a test calls `server.use()` with a custom handler
- **THEN** that handler SHALL override the default for the duration of that test only

### Requirement: Typed fixture factories

The fixture system SHALL provide factory functions for all API response types that return typed data matching the TypeScript interfaces in `src/api/`.

#### Scenario: Queue item factory
- **WHEN** `makeQueueItem({ status: QueueStatus.Processing })` is called
- **THEN** it SHALL return a complete `QueueItem` with all required fields and the specified status

#### Scenario: Factory defaults are valid
- **WHEN** `makeQueueItem()` is called with no overrides
- **THEN** the returned object SHALL have valid values for all fields (non-empty title, valid enum values, positive bytes)

### Requirement: SSE mock helper

The test infrastructure SHALL provide a helper for mocking EventSource connections used by `useQueueStream` and `useLogStream`.

#### Scenario: Initial SSE state
- **WHEN** a test configures SSE with initial queue data
- **THEN** the Activity or Home page SHALL receive the data via the mocked EventSource

#### Scenario: SSE update push
- **WHEN** a test pushes a new queue state via the SSE helper
- **THEN** the page SHALL reactively update to reflect the new state

### Requirement: CI integration

The test suite SHALL be runnable in GitHub Actions without manual browser installation.

#### Scenario: CI execution
- **WHEN** the Playwright tests run in CI
- **THEN** browsers SHALL be installed via `npx playwright install --with-deps`
- **AND** the test results SHALL be reported in the CI output
