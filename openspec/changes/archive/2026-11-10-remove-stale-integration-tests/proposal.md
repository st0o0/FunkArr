## Why

Commit `b8df181d` removed SABnzbd pause/resume/priority/swap controller arms and internal API per-item pause/resume endpoints, but left 5 integration tests that exercise those deleted routes. These tests timeout on `ExpectMsg` (3s) because the HTTP layer now returns error responses before reaching the actor, causing CI failures on every PR (including the release-please v0.4.0 PR #219).

## What Changes

- Delete 5 stale integration tests:
  - `DownloadReorderTests.Priority_Success_ReturnsOk` — tests `POST /api/downloads/queue/{id}/priority` with integer binding that no longer matches the validation guard added in b8df181d
  - `SabnzbdQueueTests.Queue_priority_returns_success` — tests `mode=queue&name=priority` (removed arm)
  - `SabnzbdQueueTests.Queue_swap_returns_success` — tests `mode=queue&name=switch` (removed arm)
  - `SabnzbdDownloadTests.Pause_returns_success` — tests `mode=pause` (removed arm)
  - `SabnzbdDownloadTests.Resume_returns_success` — tests `mode=resume` (removed arm)
- Update specs to remove requirements for deleted endpoints/operations

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `download-priority`: Remove requirements for SABnzbd queue priority operation, SABnzbd queue switch operation, DownloadPriority.Paused enum value, per-item pause via priority -2, and internal API pause/resume endpoints
- `sabnzbd-download-api`: Remove mention of pause/resume modes and queue priority/switch subcommands from the SABnzbd controller spec
- `integration-test-downloads`: Remove "Pause returns success" scenario from the integration test spec
- `download-queue-reorder`: Remove SABnzbd-specific queue priority/swap scenarios (internal API endpoints for priority/swap still exist)

## Impact

- `src/FunkArr.IntegrationTests/Api/DownloadReorderTests.cs` — remove 1 test method
- `src/FunkArr.IntegrationTests/Arr/SabnzbdQueueTests.cs` — remove 2 test methods
- `src/FunkArr.IntegrationTests/Arr/SabnzbdDownloadTests.cs` — remove 2 test methods
- `openspec/specs/download-priority/spec.md` — remove Paused-related requirements
- `openspec/specs/sabnzbd-download-api/spec.md` — remove pause/resume/priority/switch references
- `openspec/specs/integration-test-downloads/spec.md` — remove stale scenario
- `openspec/specs/download-queue-reorder/spec.md` — remove SABnzbd-specific scenarios
