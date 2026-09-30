## MODIFIED Requirements

### Requirement: RuleSetUpdater polls GitHub Releases API on a 30-minute interval
The RuleSetUpdater actor SHALL check the GitHub Releases API for new community
ruleset versions on a 30-minute interval using `IWithTimers`. It SHALL use typed
`GitHubRelease` models with `ReadFromJsonAsync` for deserialization instead of raw
`JsonElement` traversal. It SHALL use the `IDataFiles` abstraction for all filesystem
operations including directory existence checks.

#### Scenario: Successful update check with typed models
- **WHEN** the timer fires and the GitHub API returns releases
- **THEN** FindRelease SHALL deserialize the response as `GitHubRelease[]` via `ReadFromJsonAsync`
- **THEN** it SHALL find the first release with `TagName` starting with `rulesets-v`
- **THEN** it SHALL find the asset with `Name` equal to `rulesets.zip`
- **THEN** it SHALL return the tag, version, and `BrowserDownloadUrl`

#### Scenario: No matching release
- **WHEN** no release has a `TagName` starting with `rulesets-v`
- **THEN** FindRelease SHALL return null

#### Scenario: Release without rulesets.zip asset
- **WHEN** the matching release has no asset named `rulesets.zip`
- **THEN** FindRelease SHALL return a result with null AssetUrl
- **THEN** the updater SHALL log a warning and skip the update

#### Scenario: Pinned version not found
- **WHEN** the configured version is not `latest` and no matching release exists
- **THEN** FindRelease SHALL log a warning and return null

#### Scenario: ZIP extraction directory check uses IDataFiles
- **WHEN** the downloaded ZIP is extracted to a temp directory
- **THEN** the check for a nested `rulesets/` subdirectory SHALL use `_dataFiles.Exists()`
  instead of raw `Directory.Exists()`

#### Scenario: GitHub API error
- **WHEN** the GitHub API returns a non-success status code
- **THEN** FindRelease SHALL log a warning and return null without throwing
