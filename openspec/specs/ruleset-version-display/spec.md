# ruleset-version-display Specification

## Purpose
TBD - created by archiving change show-ruleset-version. Update Purpose after archive.
## Requirements
### Requirement: System version endpoint
The system SHALL expose `GET /api/system/version` that returns a JSON object with `appVersion` (string, assembly version) and `communityRulesetVersion` (string or null, read from `version.txt` via `IDataFiles`). The endpoint SHALL use output caching. When `version.txt` does not exist, `communityRulesetVersion` SHALL be `null`.

#### Scenario: Version with community rulesets installed
- **WHEN** `GET /api/system/version` is called and `version.txt` contains "1.2.0"
- **THEN** the response is 200 with `{ "appVersion": "0.1.3", "communityRulesetVersion": "1.2.0" }`

#### Scenario: Version before first ruleset sync
- **WHEN** `GET /api/system/version` is called and `version.txt` does not exist
- **THEN** the response is 200 with `communityRulesetVersion` as `null`

### Requirement: Sidebar version display
The sidebar footer SHALL display the community ruleset version below the Collapse toggle. The version text SHALL use `text-xs text-text-muted` styling. When the sidebar is collapsed, the version SHALL be hidden. When `communityRulesetVersion` is null, no version text SHALL be rendered.

#### Scenario: Version visible in expanded sidebar
- **WHEN** the sidebar is expanded and the community ruleset version is "1.2.0"
- **THEN** the sidebar footer shows "Rulesets v1.2.0" in muted text below the collapse toggle

#### Scenario: Version hidden in collapsed sidebar
- **WHEN** the sidebar is collapsed
- **THEN** no version text is visible

#### Scenario: Version hidden when not available
- **WHEN** the community ruleset version is null
- **THEN** no version text is rendered in the sidebar

### Requirement: RuleSets page version badge
The RuleSets list page SHALL display the community ruleset version as a subtle badge near the page title. The badge SHALL use `text-xs text-text-secondary` styling. When `communityRulesetVersion` is null, the badge SHALL not be rendered.

#### Scenario: Version badge on RuleSets page
- **WHEN** the user navigates to `/rulesets` and the community version is "1.2.0"
- **THEN** a "v1.2.0" badge is visible near the page title

#### Scenario: No badge when version unavailable
- **WHEN** the user navigates to `/rulesets` and the community version is null
- **THEN** no version badge is rendered

