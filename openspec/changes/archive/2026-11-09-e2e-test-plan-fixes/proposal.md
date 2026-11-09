## Why

Across 6 E2E test runs (Nov 3–9), ~7 tests fail every run due to test plan ordering and documentation issues — not code bugs. The same failures repeat because the execution order has race conditions against the auto-crawl timer, state dependencies between parallel subagents, missing API format specs, and queue saturation blocking Radarr verification.

## What Changes

- **Reorder execution**: Move empty-feed validation tests (32.6-32.11, 30.3-30.4) to a new Step 0 — API-only checks that run within 30s of container startup, before auto-crawl fires at ~43-60s.
- **Fix state dependency**: Remove 23.7 (export) from the Step 39 API smoke test batch — it's already tested in Step 14 where the local overlay exists. Prevents the "no local overlay" false failure.
- **Document API format**: Add explicit request body format to test 28.8 (priority) — `{"priority": 1}` with integer enum values (0=Normal, 1=High, 2=Low). Agents currently guess string "High".
- **Isolate Radarr search**: Move Radarr search flow (section 26) to run before Sonarr bulk searches, giving it a clean empty queue for download verification.
- **Add timing constraint annotations**: Mark time-sensitive tests with explicit deadlines relative to container startup.

## Capabilities

### New Capabilities

- `e2e-execution-order`: Revised test execution order that eliminates race conditions, state conflicts, and queue saturation issues.

### Modified Capabilities

(none — no spec-level behavior changes, only test plan documentation)

## Impact

- **E2E-TEST-PLAN.md**: Execution order section rewritten, test descriptions updated for 28.8, annotations added for time-sensitive steps, 23.7 exclusion note in Step 39.
- **No code changes**: All fixes are in the test plan document only.
- **Expected result**: Recurring ~7 avoidable failures per run reduced to ~0.
