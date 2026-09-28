# ruleset-updater

## Purpose

RuleSetUpdater actor that polls GitHub Releases API for community ruleset updates, downloads ZIP assets, and extracts them atomically to the community rulesets directory.

## Requirements

### Requirement: RuleSetUpdater polls GitHub Releases API on a 30-minute interval
The RuleSetUpdater actor SHALL check the GitHub Releases API for new community
ruleset versions on a 30-minute interval using `IWithTimers`. It SHALL use typed
`GitHubRelease` models with `ReadFromJsonAsync` for deserialization instead of raw
`JsonElement` traversal. It SHALL use the `IDataFiles` abstraction for all filesystem
operations including directory existence checks.

#### Scenario: First check on startup
- **WHEN** the RuleSetUpdater actor starts
- **THEN** it SHALL send `CheckForUpdates` to self immediately

#### Scenario: Periodic polling
- **WHEN** a `CheckForUpdates` completes (success or failure)
- **THEN** the actor SHALL schedule the next `CheckForUpdates` after 30 minutes

#### Scenario: Refresh disabled
- **WHEN** `FunkArrOptions.RuleSetRefreshEnabled` is `false`
- **THEN** the RuleSetUpdater SHALL NOT schedule any `CheckForUpdates` messages and SHALL log at Info level that refresh is disabled

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

### Requirement: RuleSetUpdater discovers releases by tag prefix
The RuleSetUpdater SHALL query `GET https://api.github.com/repos/{RuleSetRepository}/releases` and filter releases by the tag prefix `community-rulesets-`.

#### Scenario: Discover latest release
- **WHEN** `RuleSetVersion` is `"latest"`
- **THEN** the actor SHALL select the first release whose tag starts with `community-rulesets-v`

#### Scenario: Discover pinned release
- **WHEN** `RuleSetVersion` is `"1.2.0"`
- **THEN** the actor SHALL find the release with tag `community-rulesets-v1.2.0`

#### Scenario: Pinned version not found
- **WHEN** `RuleSetVersion` is `"99.0.0"` and no matching release exists
- **THEN** the actor SHALL log a warning and retain existing community rulesets

### Requirement: RuleSetUpdater compares local version before downloading
The RuleSetUpdater SHALL read `DataPaths.RuleSetVersion` using `IDataFiles.Exists()` and `IDataFiles.ReadText()` and compare with the remote release version. If versions match, the download SHALL be skipped.

#### Scenario: Version matches - skip download
- **WHEN** `IDataFiles.ReadText(dataPaths.RuleSetVersion)` returns `"1.0.0"` and the latest remote release is `community-rulesets-v1.0.0`
- **THEN** the actor SHALL skip the download and log at Debug level

#### Scenario: Version differs - download
- **WHEN** `IDataFiles.ReadText(dataPaths.RuleSetVersion)` returns `"1.0.0"` and the latest remote release is `community-rulesets-v1.1.0`
- **THEN** the actor SHALL download and extract the new version

#### Scenario: No local version file
- **WHEN** `IDataFiles.Exists(dataPaths.RuleSetVersion)` returns false
- **THEN** the actor SHALL always download

### Requirement: RuleSetUpdater downloads ZIP asset from release
The RuleSetUpdater SHALL download the `community-rulesets.zip` asset from the matching GitHub release.

#### Scenario: Download ZIP asset
- **WHEN** a matching release is found with a `community-rulesets.zip` asset
- **THEN** the actor SHALL download the ZIP file

#### Scenario: No ZIP asset on release
- **WHEN** a matching release exists but has no `community-rulesets.zip` asset
- **THEN** the actor SHALL log a warning and retain existing community rulesets

### Requirement: RuleSetUpdater extracts ZIP atomically
The RuleSetUpdater SHALL extract downloaded ZIP archives atomically using `IDataFiles`: create a temp directory via `IDataFiles.CreateDirectory()` in `DataPaths.Temp`, extract the ZIP there, then call `IDataFiles.ReplaceDirectory()` to swap with `DataPaths.CommunityRuleSets`. If extraction fails, the existing directory SHALL remain untouched.

#### Scenario: Successful extraction
- **WHEN** a valid ZIP is downloaded
- **THEN** the actor SHALL call `IDataFiles.CreateDirectory()` to create a temp dir under `DataPaths.Temp`
- **AND** extract the ZIP to the temp dir
- **AND** call `IDataFiles.ReplaceDirectory(tempDir, dataPaths.CommunityRuleSets)` to atomically swap
- **AND** the old contents SHALL be deleted

#### Scenario: Corrupted ZIP
- **WHEN** a downloaded ZIP is corrupted or incomplete
- **THEN** the actor SHALL log an error, call `IDataFiles.Remove(tempDir)` to clean up, and retain the existing community rulesets

#### Scenario: Disk full during extraction
- **WHEN** extraction fails due to disk space
- **THEN** the actor SHALL log an error and retain the existing community rulesets (no partial state)

### Requirement: RuleSetUpdater writes version.txt after extraction
After a successful extraction, the RuleSetUpdater SHALL write the version using `IDataFiles.WriteText(dataPaths.RuleSetVersion, version)`.

#### Scenario: Version file written after refresh
- **WHEN** a ZIP from release `community-rulesets-v1.1.0` is successfully extracted
- **THEN** `IDataFiles.WriteText(dataPaths.RuleSetVersion, "1.1.0")` SHALL be called

### Requirement: RuleSetUpdater sends User-Agent header
All GitHub API requests SHALL include a `User-Agent` header with value `FunkArr/{version}` where version is the application assembly version.

#### Scenario: User-Agent sent
- **WHEN** the actor queries the GitHub Releases API
- **THEN** the HTTP request SHALL include header `User-Agent: FunkArr/{version}`

### Requirement: RuleSetUpdater handles API failures gracefully
The RuleSetUpdater SHALL handle HTTP failures without crashing. Failed checks SHALL log a warning and retain existing community rulesets.

#### Scenario: API unreachable
- **WHEN** the GitHub API is unreachable (network error, DNS failure)
- **THEN** the actor SHALL log a warning and schedule the next check normally

#### Scenario: Rate limited
- **WHEN** the GitHub API returns HTTP 403 with rate limit headers
- **THEN** the actor SHALL log a warning and schedule the next check normally

#### Scenario: Server error
- **WHEN** the GitHub API returns HTTP 5xx
- **THEN** the actor SHALL log a warning and schedule the next check normally

### Requirement: RuleSetUpdater configuration options
The system SHALL support configuration options for the GitHub release refresh mechanism via `FunkArrOptions`.

#### Scenario: RuleSetRepository default
- **WHEN** `FunkArr__RuleSetRepository` is not set
- **THEN** the system SHALL default to `"st0o0/funkarr"`

#### Scenario: RuleSetVersion default
- **WHEN** `FunkArr__RuleSetVersion` is not set
- **THEN** the system SHALL default to `"latest"`

#### Scenario: RuleSetRefreshEnabled default
- **WHEN** `FunkArr__RuleSetRefreshEnabled` is not set
- **THEN** the system SHALL default to `true`

#### Scenario: Custom repository
- **WHEN** `FunkArr__RuleSetRepository` is set to `"myorg/my-rulesets"`
- **THEN** the actor SHALL query that repository's releases for community rulesets

### Requirement: Named HttpClient for GitHub API
The system SHALL register a named HttpClient `"GitHub"` in DI with `User-Agent` header and `Accept: application/vnd.github+json` header.

#### Scenario: HttpClient registered
- **WHEN** the application starts
- **THEN** a named HttpClient `"GitHub"` SHALL be available with base address `https://api.github.com/` and appropriate headers
