## ADDED Requirements

### Requirement: Time-critical tests run before auto-crawl window

The test plan SHALL include a Step 0 that runs empty-feed validation tests via API calls only (no browser), immediately after the container responds on port 6969, within 30 seconds of container health.

#### Scenario: Validation fallback items tested before auto-crawl
- **WHEN** the E2E test run starts and the container is healthy
- **THEN** tests 32.6-32.11 (Newznab validation fallback items) execute as API calls in Step 0, before any browser navigation or setup wizard steps

#### Scenario: Feed empty state verified via API
- **WHEN** Step 0 runs within 30s of container startup
- **THEN** tests 30.3, 30.4 verify empty feed state via `GET /api/feed` (API check), and 31.1+31.5 verify ingest status is "never"

#### Scenario: Browser feed check is best-effort
- **WHEN** the browser-based feed page check runs later (Step 9)
- **THEN** the test plan notes that auto-crawl may have fired; a populated feed at this point is not a failure if Step 0 API checks passed

### Requirement: Export test runs only when local overlay exists

The test plan SHALL test 23.7 (export) only in Step 14, where the local overlay is known to exist. Step 39 (API smoke tests) SHALL exclude 23.7.

#### Scenario: Export tested before overlay deletion
- **WHEN** Step 14 executes
- **THEN** export (section 11 / test 23.7) runs before the overlay is deleted in the same step

#### Scenario: API smoke tests skip export
- **WHEN** Step 39 (API smoke tests) executes section 23
- **THEN** test 23.7 is skipped with a note "already tested in step 14"

### Requirement: API body formats are explicitly documented

The test plan SHALL specify exact request body format (JSON structure, data types) for all API tests that accept a request body with enum values.

#### Scenario: Priority endpoint documents integer enum
- **WHEN** an agent reads test 28.8
- **THEN** the test description includes the exact body `{"priority": 1}` and the enum mapping (0=Normal, 1=High, 2=Low)

### Requirement: Radarr search has isolated queue window

The test plan SHALL schedule the Radarr search flow (section 26) before Sonarr bulk searches, so Radarr downloads execute against an empty queue.

#### Scenario: Radarr search before Sonarr queue saturation
- **WHEN** the execution order reaches the download flow tests
- **THEN** Radarr search (26.1-26.7) runs with pipeline active and an empty queue, before any Sonarr episode searches are triggered

#### Scenario: Pipeline re-paused after Radarr completion
- **WHEN** Radarr downloads complete
- **THEN** the pipeline is paused again before Sonarr searches begin, preserving the paused-queue setup for queue operation tests
