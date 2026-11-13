# contribution-scaffolding Specification

## Purpose
TBD - created by archiving change developer-docs. Update Purpose after archive.
## Requirements
### Requirement: CONTRIBUTING.md is a short entry point
CONTRIBUTING.md SHALL contain a short welcome, links to the docs site's developer pages (Contributing, Local development, Rulesets) in English and German, the commit-message rule in one line, and links to the Code of Conduct and SECURITY.md. It SHALL NOT duplicate conventions or command lists.

#### Scenario: Links resolve to the docs site
- **WHEN** a contributor opens CONTRIBUTING.md on GitHub
- **THEN** it links to `https://funkarr.st0o0.net/en/develop/` and `https://funkarr.st0o0.net/develop/`

### Requirement: README contains no hand-maintained developer lists
The README SHALL NOT contain a per-project test command list, a full environment-variable table or a hard-coded current version number. It SHALL link to the docs site's configuration page and developer pages instead.

#### Scenario: No test project list
- **WHEN** the README is read
- **THEN** it contains no `dotnet run --project src/...Tests...` lines and links to the developer pages instead

#### Scenario: No stale version
- **WHEN** a new release is cut
- **THEN** no README text needs editing to stay accurate about the current version

### Requirement: Issue templates
The repository SHALL provide GitHub issue forms for bug reports, feature requests and ruleset requests. The bug form SHALL ask for the FunkArr version, deployment (image tag), the involved *arr app, steps to reproduce and logs. The ruleset form SHALL ask for the show or movie name, broadcaster, an example Mediathek title and the TVDB/TMDB ID.

#### Scenario: Bug report requires version
- **WHEN** a user opens a new bug report
- **THEN** the version field is required

#### Scenario: Ruleset request form available
- **WHEN** a user chooses "New issue"
- **THEN** a "Ruleset request" form is offered next to bug report and feature request

### Requirement: Pull request template
The repository SHALL provide a pull request template with a summary, a linked issue or OpenSpec change, and a checklist: Conventional Commits, tests added or updated, zero warnings and `dotnet format` passing, AGENTS.md updated when conventions changed, architecture model updated for actor or domain changes, and docs updated in German and English when behaviour or developer-facing structure changed.

#### Scenario: Both locales reminder
- **WHEN** a contributor opens a pull request
- **THEN** the checklist asks whether the docs were updated in both German and English

### Requirement: Manual E2E plan lives with the UI tests
The manual E2E test plan SHALL live under `src/FunkArr.UI/e2e/`, and every reference to it (agent skills, docs) SHALL point there.

#### Scenario: No stale reference
- **WHEN** the repository is searched for `E2E-TEST-PLAN.md`
- **THEN** every reference outside archived OpenSpec changes resolves to `src/FunkArr.UI/e2e/E2E-TEST-PLAN.md`

