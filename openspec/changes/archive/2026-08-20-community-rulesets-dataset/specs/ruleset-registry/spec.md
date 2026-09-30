## MODIFIED Requirements

### Requirement: Community refresh
The system SHALL periodically refresh community rulesets. When `RuleSetRefreshMode` is `"github-release"`, the system SHALL query the GitHub Releases API for the configured repository, download the ZIP asset, extract it atomically, and reload the in-memory index. When `RuleSetRefreshMode` is `"legacy-url"`, the system SHALL use the existing behavior of fetching `RuleSetSourceUrl` and parsing via `CommunityRuleSetParser`. The refresh interval SHALL default to 60 minutes.

#### Scenario: GitHub release refresh (default)
- **WHEN** `RuleSetRefreshMode` is `"github-release"` and the refresh timer fires
- **THEN** the system SHALL query the GitHub Releases API, download the ZIP if a newer version is available, extract to `community/`, and reload the index

#### Scenario: Legacy URL refresh
- **WHEN** `RuleSetRefreshMode` is `"legacy-url"` and the refresh timer fires
- **THEN** the system SHALL fetch `RuleSetSourceUrl`, parse via `CommunityRuleSetParser`, write files to `community/`, and reload the index

#### Scenario: Refresh failure (GitHub release mode)
- **WHEN** the GitHub API is unreachable during a refresh attempt
- **THEN** the system SHALL log a warning and retain the existing community files and index entries

#### Scenario: Refresh failure (legacy URL mode)
- **WHEN** the source URL is unreachable during a refresh attempt
- **THEN** the system SHALL log a warning and retain the existing community files and index entries

### Requirement: Configurable source URL
The community source SHALL be configurable. In `github-release` mode, `FunkArr__RuleSetRepository` (default `"st0o0/funkarr"`) and `FunkArr__RuleSetVersion` (default `"latest"`) SHALL control which release to fetch. In `legacy-url` mode, `FunkArr__RuleSetSourceUrl` SHALL control the fetch URL. `FunkArr__RuleSetRefreshMode` (default `"github-release"`) SHALL select between the two modes.

#### Scenario: GitHub release mode with defaults
- **WHEN** no `RuleSetRefreshMode`, `RuleSetRepository`, or `RuleSetVersion` is configured
- **THEN** the system SHALL use github-release mode, querying `st0o0/funkarr` for the latest community-rulesets release

#### Scenario: Pinned version
- **WHEN** `RuleSetVersion` is set to `"1.0.0"`
- **THEN** the system SHALL fetch the `community-rulesets-v1.0.0` release specifically

#### Scenario: Custom repository
- **WHEN** `RuleSetRepository` is set to `"myorg/my-rulesets"`
- **THEN** the system SHALL query that repository's releases

#### Scenario: Legacy URL mode
- **WHEN** `RuleSetRefreshMode` is `"legacy-url"` and `RuleSetSourceUrl` is set to a custom URL
- **THEN** the system SHALL fetch community rulesets from that URL using the legacy parser

#### Scenario: Default source URL (legacy mode)
- **WHEN** `RuleSetRefreshMode` is `"legacy-url"` and no `RuleSetSourceUrl` is set
- **THEN** the system SHALL use the default GitHub raw URL
