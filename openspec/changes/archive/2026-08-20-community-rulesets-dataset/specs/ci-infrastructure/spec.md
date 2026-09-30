## MODIFIED Requirements

### Requirement: Release-please config matches Njord conventions
The `release-please-config.json` MUST include a `$schema` field and two packages: the root package (`.`) with `include-component-in-tag: false`, and a `data/community` package with component `community-rulesets`, `include-component-in-tag: true`, release-type `simple`, and `separate-pull-requests: true`. Both packages MUST share the same `changelog-sections` (feat, fix, perf, docs, refactor visible; chore, test, ci, build hidden; deps visible). The root package MUST use `extra-files` with `generic` type for `src/Directory.Build.props`.

#### Scenario: Changelog sections produce clean output
- **WHEN** a release is created with commits of types feat, fix, chore, test, ci, deps
- **THEN** the changelog shows sections for Features, Bug Fixes, and Dependencies, and hides chore, test, and ci commits

#### Scenario: Version bump uses comment marker
- **WHEN** release-please bumps the app version
- **THEN** it updates the `<Version>` line in `src/Directory.Build.props` using the `<!-- x-release-please-version -->` comment marker via the `generic` extra-files type

#### Scenario: Community rulesets component creates separate PR
- **WHEN** a commit with scope `(rulesets)` is pushed to main
- **THEN** release-please SHALL create a separate PR for the `community-rulesets` component

#### Scenario: Community rulesets tag includes component name
- **WHEN** the community-rulesets release PR is merged
- **THEN** the GitHub Release SHALL have a tag like `community-rulesets-v1.0.0`

#### Scenario: Community rulesets version tracked in version.txt
- **WHEN** release-please bumps the community-rulesets version
- **THEN** it SHALL update `data/community/version.txt`

## ADDED Requirements

### Requirement: Ruleset ZIP asset workflow step
The release workflow SHALL include a step that packages `data/community/rulesets/*.json` into a `community-rulesets.zip` and attaches it to the community-rulesets GitHub Release using `softprops/action-gh-release@v2`.

#### Scenario: ZIP attached on community-rulesets release
- **WHEN** release-please creates a `community-rulesets-*` release
- **THEN** the workflow SHALL zip the rulesets and attach `community-rulesets.zip` to that release

#### Scenario: ZIP not created for app-only release
- **WHEN** release-please creates an app release (no community-rulesets release)
- **THEN** the workflow SHALL NOT attempt to package or upload a ruleset ZIP

### Requirement: Docker image includes embedded rulesets
The Dockerfile SHALL copy `data/community/rulesets/` into the image at `/app/data/rulesets/community/` so that the community layer is available without network access.

#### Scenario: Rulesets present in built image
- **WHEN** the Docker image is built
- **THEN** the image SHALL contain the community ruleset JSON files at `/app/data/rulesets/community/`
