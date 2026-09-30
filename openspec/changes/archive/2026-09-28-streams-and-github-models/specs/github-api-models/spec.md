## ADDED Requirements

### Requirement: Typed GitHub release models
The system SHALL provide typed deserialization records for the GitHub Releases API
response. The models SHALL be `internal sealed record` types in `FunkArr.RuleSet`
namespace with `JsonPropertyName` attributes.

#### Scenario: GitHubRelease record
- **WHEN** the GitHub releases API response is deserialized
- **THEN** `GitHubRelease` SHALL have properties `TagName` (string?) and `Assets` (GitHubReleaseAsset[]?)

#### Scenario: GitHubReleaseAsset record
- **WHEN** a release asset is deserialized
- **THEN** `GitHubReleaseAsset` SHALL have properties `Name` (string?) and `BrowserDownloadUrl` (string?)

#### Scenario: Unknown JSON properties ignored
- **WHEN** the GitHub API returns fields not modeled (e.g. `id`, `body`, `created_at`)
- **THEN** deserialization SHALL succeed without error (System.Text.Json default behavior)

### Requirement: Models scoped to RuleSet domain
The GitHub API models SHALL be `internal` and live in `FunkArr.RuleSet/GitHubModels.cs`.
They SHALL NOT be placed in Messages or Core projects.

#### Scenario: Visibility
- **WHEN** other domain projects attempt to reference GitHub models
- **THEN** compilation SHALL fail because the types are `internal` to FunkArr.RuleSet
