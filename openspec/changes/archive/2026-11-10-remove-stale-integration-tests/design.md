## Context

Commit `b8df181d` intentionally removed SABnzbd pause/resume modes and queue priority/switch subcommands as "UI-only" operations not needed by Sonarr/Radarr. It also removed internal API per-item pause/resume endpoints and the `DownloadPriority.Paused` enum value. The corresponding integration tests and spec requirements were not cleaned up.

## Goals / Non-Goals

**Goals:**
- Unblock CI by removing tests that exercise deleted endpoints
- Bring specs in sync with the current codebase state

**Non-Goals:**
- Changing any runtime code — only tests and specs are touched
- Re-evaluating whether the removed endpoints should come back

## Decisions

1. **Delete stale test methods, not entire files.** Each affected test file has other valid tests that still pass. Only the specific methods testing removed routes are deleted.

2. **Update main specs via delta specs.** The change produces delta specs for 4 existing capabilities, removing requirements that no longer match the codebase.

3. **DownloadReorderTests.Priority_Success_ReturnsOk** — this test sends `{ "priority": 1 }` as an integer. After `b8df181d` added the `QueuePriority` validation guard (`is not (Low or Normal or High)`), the integer 1 doesn't bind correctly to the enum, hitting the 400 path before reaching the actor. The endpoint itself still exists but this specific test was written for the pre-validation behavior. Remove the test; the `Priority_Invalid_Returns400` test already covers the validation guard path.

## Risks / Trade-offs

- **Reduced SABnzbd coverage** — removing these tests means no coverage for pause/resume/priority/switch on the SABnzbd adapter. This is fine because the endpoints don't exist anymore.
- **Priority endpoint test gap** — removing the happy-path priority test leaves only the invalid-input test. If the priority endpoint needs a new happy-path test, it should send the enum as a string ("High"/"Normal"/"Low") matching the current `QueuePriority` binding. This is out of scope for this cleanup.
