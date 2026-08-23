# GitHub Release Refresh

## Purpose

GitHub Releases API client for discovering, downloading, and atomically extracting community ruleset releases, with version checking and configuration options.

## Requirements

### Requirement: GitHub Releases API client
The system SHALL provide a service that queries the GitHub Releases API to discover community ruleset releases, download ZIP assets, and extract them to the community rulesets directory.

#### Scenario: Discover latest release
- **WHEN** `RuleSetVersion` is `"latest"`
- **THEN** the system SHALL query `GET https://api.github.com/repos/{RuleSetRepository}/releases` and select the first release whose tag starts with `community-rulesets-`

#### Scenario: Discover pinned release
- **WHEN** `RuleSetVersion` is `"1.2.0"`
- **THEN** the system SHALL find the release with tag `community-rulesets-v1.2.0`

#### Scenario: Pinned version not found
- **WHEN** `RuleSetVersion` is `"99.0.0"` and no matching release exists
- **THEN** the system SHALL log a warning and retain existing community rulesets

#### Scenario: Download ZIP asset
- **WHEN** a matching release is found
- **THEN** the system SHALL download the `community-rulesets.zip` asset from the release

#### Scenario: No ZIP asset on release
- **WHEN** a matching release exists but has no `community-rulesets.zip` asset
- **THEN** the system SHALL log a warning and retain existing community rulesets

### Requirement: Atomic extraction
The system SHALL extract downloaded ZIP archives atomically: extract to a temporary directory first, then swap with the existing community directory. If extraction fails, the existing directory SHALL remain untouched.

#### Scenario: Successful extraction
- **WHEN** a valid ZIP is downloaded
- **THEN** the system SHALL extract to a temp directory, swap it with `rulesets/community/`, and delete the old directory

#### Scenario: Corrupted ZIP
- **WHEN** a downloaded ZIP is corrupted or incomplete
- **THEN** the system SHALL log an error, clean up the temp directory, and retain the existing community rulesets

#### Scenario: Disk full during extraction
- **WHEN** extraction fails due to disk space
- **THEN** the system SHALL log an error and retain the existing community rulesets (no partial state)

### Requirement: Version check before download
The system SHALL compare the local community ruleset version with the remote release version before downloading. If the versions match, the download SHALL be skipped.

#### Scenario: Version matches -- skip download
- **WHEN** the local `version.txt` contains `1.0.0` and the latest remote release is `community-rulesets-v1.0.0`
- **THEN** the system SHALL skip the download and log at Debug level

#### Scenario: Version differs -- download
- **WHEN** the local `version.txt` contains `1.0.0` and the latest remote release is `community-rulesets-v1.1.0`
- **THEN** the system SHALL download and extract the new version

#### Scenario: No local version file
- **WHEN** no `version.txt` exists in the community rulesets directory (first run or embedded-only)
- **THEN** the system SHALL always download

### Requirement: Version file update after extraction
After a successful extraction, the system SHALL write a `version.txt` file in the community rulesets directory containing the version from the downloaded release tag.

#### Scenario: Version file written after refresh
- **WHEN** a ZIP from release `community-rulesets-v1.1.0` is successfully extracted
- **THEN** a `version.txt` containing `1.1.0` SHALL exist in the community rulesets directory

### Requirement: Configuration options
The system SHALL support the following configuration options for the GitHub release refresh mechanism.

#### Scenario: RuleSetRepository default
- **WHEN** `FunkArr__RuleSet__Repository` is not set
- **THEN** the system SHALL default to `"st0o0/funkarr"`

#### Scenario: RuleSetVersion default
- **WHEN** `FunkArr__RuleSet__Version` is not set
- **THEN** the system SHALL default to `"latest"`

#### Scenario: Custom repository
- **WHEN** `FunkArr__RuleSet__Repository` is set to `"myorg/my-rulesets"`
- **THEN** the system SHALL query that repository's releases for community rulesets

### Requirement: User-Agent header
All GitHub API requests SHALL include a `User-Agent` header with value `FunkArr/{version}` where version is the application version.

#### Scenario: User-Agent sent
- **WHEN** the system queries the GitHub Releases API
- **THEN** the HTTP request SHALL include header `User-Agent: FunkArr/{version}`
