# CI Infrastructure

## Purpose

Defines the CI/CD pipeline configuration, linting, dependency management, security scanning, and developer tooling conventions for the project.

## Requirements

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

### Requirement: Commitlint enforces conventional commits
The CI pipeline MUST run commitlint on pull requests using `wagoid/commitlint-github-action`. A `commitlint.config.mjs` MUST exist with the same type-enum and relaxed rules as Njord (no subject-case enforcement, no body/footer line length limits, dependabot ignored).

#### Scenario: Valid conventional commit passes
- **WHEN** a PR contains commits with valid types (feat, fix, chore, refactor, deps, etc.)
- **THEN** the commitlint CI job passes

#### Scenario: Invalid commit type fails
- **WHEN** a PR contains a commit with an unrecognized type (e.g. "update: something")
- **THEN** the commitlint CI job fails

### Requirement: Dependabot with grouped updates
A `.github/dependabot.yml` MUST configure weekly updates for nuget (with groups: akka, testing, servus), github-actions, and docker ecosystems. All MUST use `deps` as commit-message prefix.

#### Scenario: NuGet updates are grouped
- **WHEN** dependabot detects updates for multiple Akka packages
- **THEN** they are combined into a single PR under the `akka` group

### Requirement: Security scanning workflow
A `.github/workflows/security.yml` MUST run Trivy container scanning (SARIF output, non-blocking) and NuGet vulnerability auditing. It MUST trigger on PRs touching Dockerfile/packages/csproj, on a weekly schedule, and on manual dispatch.

#### Scenario: Trivy scan uploads results
- **WHEN** the security workflow runs
- **THEN** Trivy scans the built Docker image and uploads SARIF to GitHub's Security tab

#### Scenario: NuGet audit lists vulnerabilities
- **WHEN** the security workflow runs
- **THEN** `dotnet list package --vulnerable --include-transitive` runs and reports findings

### Requirement: Dev-build workflow for PR docker images
A `.github/workflows/dev-build.yml` MUST build multi-arch Docker images (amd64, arm64) from PRs labeled `dev-build`, gated by a `dev` environment. Images MUST be tagged `pr-<N>` and `dev-<shortsha>`, and the workflow MUST comment the pull command on the PR.

#### Scenario: Labeled PR triggers dev build
- **WHEN** a PR has the `dev-build` label and passes environment approval
- **THEN** multi-arch Docker images are built, pushed to GHCR, and the pull command is commented on the PR

### Requirement: Hadolint configuration
A `.hadolint.yaml` MUST exist at the repo root with `failure-threshold: warning`. The CI lint job MUST reference it via `config: .hadolint.yaml`.

#### Scenario: Hadolint uses config file
- **WHEN** the CI lint job runs hadolint
- **THEN** it uses `.hadolint.yaml` for configuration

### Requirement: CLAUDE.md project documentation
A `CLAUDE.md` MUST exist at the repo root documenting: project description, architecture, build & test commands, conventions (git, versioning, testing, C#, persistence DTOs), workflow (OpenSpec), and skill routing.

#### Scenario: CLAUDE.md reflects FunkArr specifics
- **WHEN** reading CLAUDE.md
- **THEN** it describes FunkArr's architecture (Mediathek search, download pipeline, SABnzbd/Newznab APIs, FFmpeg muxing) and not Njord's

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
