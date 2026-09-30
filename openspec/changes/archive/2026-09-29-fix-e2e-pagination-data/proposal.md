## Why

The E2E test run fails 3 pagination tests (5.10, 12.5, 12.6) because the test
execution doesn't generate enough data to exceed page boundaries. History
pagination needs >25 entries (pageSize=25) but only produces ~3. Scoring
pagination needs >20 entries (pageSize=20) but only produces ~17. These are test
setup gaps, not application bugs.

## What Changes

- **E2E-TEST-PLAN.md**: Revise step 21 ("Generate pagination data") to trigger
  15+ Sonarr searches across seasons 2020-2026 instead of "5+ more searches",
  reliably producing >20 scoring entries for Tatort.
- **E2E-TEST-PLAN.md**: Insert new step 18.5 ("Generate history pagination data")
  that uploads 20 NZB files with unreachable URLs (10x category=show, 10x
  category=movie) via SABnzbd addfile. These fail quickly and produce >25 history
  entries. Mixed categories also strengthen the category filter test (5.3).
- **E2E-TEST-PLAN.md**: Update the Setup Recipe for pagination data to reflect
  the new approach.
- **.claude/skills/e2e-verify/skill.md**: Update Phase 3 and Phase 4 instructions
  to match the new test plan steps.

## Capabilities

### New Capabilities

None. This is a test infrastructure change only.

### Modified Capabilities

None. No application behavior or requirements change.

## Impact

- `E2E-TEST-PLAN.md` - test execution order and setup recipes
- `.claude/skills/e2e-verify/skill.md` - Phase 3 and Phase 4 instructions
- No application code changes
