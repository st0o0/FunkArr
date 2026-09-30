## MODIFIED Requirements

### Requirement: Scoring history empty state
The scoring history page SHALL use the shared `EmptyState` component when no scoring history exists. The empty state SHALL show a clock/history icon, a title indicating no history exists, and a description explaining that history appears after scoring runs are triggered by Sonarr or Radarr searches.

#### Scenario: Empty scoring history
- **WHEN** the user views scoring history for a ruleset with no past runs
- **THEN** the page displays an EmptyState with icon, title, and description instead of plain text

### Requirement: Home recent activity empty state
The home page recent activity section SHALL use the shared `EmptyState` component when no recent activity exists. The empty state SHALL show a download icon, the existing "no recent activity" title, and the existing hint text explaining when downloads appear.

#### Scenario: Empty recent activity on home
- **WHEN** the user views the home page with no recent downloads
- **THEN** the recent activity section displays an EmptyState with icon, title, and description instead of plain paragraph tags
