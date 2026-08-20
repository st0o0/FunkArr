# Ruleset Release Pipeline

## Purpose

Release-please component for community rulesets with separate versioning, ZIP packaging of ruleset files as GitHub Release assets, and Docker image embedding.

## Requirements

### Requirement: Release-please component for community rulesets
The `release-please-config.json` SHALL include a second package at path `data/community` with component name `community-rulesets`, release-type `simple`, `include-component-in-tag: true`, and `separate-pull-requests: true`. The `.release-please-manifest.json` SHALL include an entry for `data/community` initialized at `0.1.0`.

#### Scenario: Ruleset commit triggers separate release PR
- **WHEN** a commit `feat(rulesets): add tatort ruleset` is pushed to main
- **THEN** release-please SHALL create a separate PR for the `community-rulesets` component, independent of the app release PR

#### Scenario: Release tag includes component name
- **WHEN** the community-rulesets release PR is merged
- **THEN** the GitHub Release SHALL have a tag like `community-rulesets-v1.0.0`

#### Scenario: App commits do not trigger ruleset release
- **WHEN** a commit `feat: add download retry` is pushed to main (no `(rulesets)` scope)
- **THEN** the community-rulesets component SHALL NOT create a release PR

#### Scenario: Changelog sections match app conventions
- **WHEN** a community-rulesets release is created
- **THEN** the `data/community/CHANGELOG.md` SHALL have the same changelog-sections as the app (Features, Bug Fixes, etc.)

### Requirement: ZIP asset packaging
The release workflow SHALL package all JSON files from `data/community/rulesets/` into a `community-rulesets.zip` archive and attach it to the GitHub Release as an asset when a community-rulesets release is created.

#### Scenario: ZIP contains only ruleset JSON files
- **WHEN** a community-rulesets release is created
- **THEN** the `community-rulesets.zip` asset SHALL contain all `*.json` files from `data/community/rulesets/` and no other files (no version.txt, no CHANGELOG.md)

#### Scenario: ZIP attached to correct release
- **WHEN** a community-rulesets release `community-rulesets-v1.0.0` is created
- **THEN** the ZIP SHALL be attached to the `community-rulesets-v1.0.0` release, not the app release

#### Scenario: ZIP asset uses softprops/action-gh-release
- **WHEN** the release workflow packages the ZIP
- **THEN** it SHALL use `softprops/action-gh-release@v2` with the existing tag to attach the asset to the already-created release

### Requirement: Docker image embeds community rulesets
The Dockerfile SHALL copy `data/community/rulesets/` into the image at `/app/data/rulesets/community/` so that FunkArr has a default community layer available without network access.

#### Scenario: Offline startup with embedded rulesets
- **WHEN** a FunkArr container starts without network access and no prior refresh
- **THEN** the community layer SHALL be populated with the rulesets embedded at build time

#### Scenario: Refresh overwrites embedded rulesets
- **WHEN** a FunkArr container starts with embedded rulesets and then a refresh downloads a newer version
- **THEN** the community layer SHALL be updated with the downloaded version, replacing the embedded files
