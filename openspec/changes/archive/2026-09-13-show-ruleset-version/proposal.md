## Why

The community ruleset version is tracked internally (version.txt) but never surfaced to the user. When debugging match issues or checking if rulesets are up-to-date, users have no way to see which version is active without SSH-ing into the container.

## What Changes

- Expose the active community ruleset version through the existing rulesets API response
- Display the version in two places in the UI:
  - **RuleSets page header**: version badge next to the page title, visible when managing rulesets
  - **Sidebar footer**: small version text below "Setup" / "Collapse", always visible as a quick reference

## Capabilities

### New Capabilities

- `ruleset-version-display`: Surface the active community ruleset version in the API and display it in the sidebar footer and RuleSets page header

### Modified Capabilities

- `ruleset-api`: Add `communityVersion` field to the rulesets list response
- `sidebar-layout`: Add version text to the sidebar footer area
- `ruleset-ui`: Show version badge in the RuleSets page header

## Impact

- **Backend**: `RuleSetApiEndpoints` — read `version.txt` via `DataPaths.RuleSetVersion` and include in response
- **Frontend**: `AppLayout.vue` — fetch and display version in sidebar footer
- **Frontend**: `RuleSetList.vue` — show version badge next to title/filters
- **API contract**: New optional `communityVersion` field in rulesets list response (non-breaking)
