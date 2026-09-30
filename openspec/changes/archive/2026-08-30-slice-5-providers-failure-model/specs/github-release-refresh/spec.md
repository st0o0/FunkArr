## RENAMED Requirements

### Requirement: GitHubReleaseClient renamed to GitHubProvider
- **FROM:** `GitHubReleaseClient` in `RuleSet/`
- **TO:** `GitHubProvider` in `Providers/GitHub/`

## ADDED Requirements

### Requirement: GitHubProvider returns IQueryResult
All public methods on `GitHubProvider` SHALL return `Task<IQueryResult>` instead of throwing.

#### Scenario: Successful release check
- **WHEN** the GitHub API returns release data
- **THEN** `GitHubProvider` SHALL return `QuerySuccess<GitHubReleaseInfo>(data)`

#### Scenario: No new release
- **WHEN** the current version is already the latest
- **THEN** `GitHubProvider` SHALL return `QuerySuccess` with a marker indicating no update needed

#### Scenario: API unreachable
- **WHEN** the GitHub API is unreachable
- **THEN** `GitHubProvider` SHALL return `QueryFailure(Transport, ...)`

#### Scenario: Rate limited
- **WHEN** the GitHub API returns 429
- **THEN** `GitHubProvider` SHALL return `QueryFailure(RateLimited, ...)`

## MODIFIED Requirements

### Requirement: RefreshActor handles QueryFailure
The `RefreshActor` SHALL handle `QueryFailure` from `GitHubProvider` by retaining existing community files and logging a warning with the failure reason.

#### Scenario: Refresh failure
- **WHEN** `GitHubProvider` returns `QueryFailure(Transport, ...)`
- **THEN** the `RefreshActor` SHALL log a warning and retain the existing community files
