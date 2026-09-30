## Context

The community ruleset version is stored in `data/rulesets/version.txt` and managed by `RuleSetUpdater`. The `DataPaths.RuleSetVersion` property provides the file path. Currently the version is only used internally to decide whether to update rulesets from GitHub. The UI has no visibility into it.

## Goals / Non-Goals

**Goals:**
- Surface the community ruleset version in the rulesets API response
- Display the version in the sidebar footer (always visible)
- Display the version in the RuleSets page header (contextual)

**Non-Goals:**
- Version history or changelog display
- Manual version pinning from the UI
- Displaying per-ruleset versioning (all community rulesets share one version)

## Decisions

### Read version via IDataFiles

Read `version.txt` through the existing `IDataFiles` abstraction rather than raw `File.ReadAllText`. This keeps the API endpoint testable and consistent with how other file reads work in the project. The `DataPaths.RuleSetVersion` path is already wired.

### Wrap version in a response envelope

Instead of adding `communityVersion` to each ruleset entry in the array, return it as a top-level field in a new response wrapper: `{ communityVersion: "1.2.0", rulesets: [...] }`. This avoids repeating the same version string 69 times and makes the contract clearer. The frontend already expects the response shape to be an array, so this is a **breaking change** to the rulesets list response — acceptable at 0.x.

### Sidebar fetches version from a lightweight endpoint

The sidebar needs the version on every page, not just `/rulesets`. Rather than calling the full `/api/rulesets` endpoint (which queries multiple actors), add a `GET /api/system/version` endpoint that returns `{ appVersion, communityRulesetVersion }`. This is cheap (one file read, one assembly version) and can be cached.

### Graceful fallback for missing version

If `version.txt` doesn't exist (fresh install before first sync), the version SHALL be `null`. The UI shows nothing in that case — no "unknown" placeholder.

## Risks / Trade-offs

- **Breaking API change** on `/api/rulesets` (array → object wrapper) — acceptable at 0.x per project convention. Consumers are only the FunkArr UI itself.
- **Extra API call from sidebar** — `GET /api/system/version` on mount. Mitigated by output caching with short TTL (the version changes at most once per ruleset sync cycle).
