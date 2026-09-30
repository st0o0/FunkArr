## MODIFIED Requirements

### Requirement: Recent matches view
The UI SHALL display recent match records fetched from `GET /api/v1/matches/recent`, using shared TypeScript types imported from `types.ts`.

#### Scenario: Show recent matches
- **WHEN** the user navigates to the matches view
- **THEN** the UI SHALL fetch from the working endpoint and display the most recent match records with topic, timestamp, and result counts

#### Scenario: Expand match record
- **WHEN** the user expands a match record
- **THEN** the UI SHALL display the individual matched traces (rule index, strategy, season, episode) and unmatched traces (failure reasons)

### Requirement: Unmatched items explorer
The UI SHALL display unmatched items fetched from the working `GET /api/v1/matches/unmatched` endpoint, using shared TypeScript types.

#### Scenario: Browse unmatched items
- **WHEN** the user views unmatched items
- **THEN** the UI SHALL fetch from the working endpoint and display unmatched items grouped by topic, sorted by group size descending

#### Scenario: Navigate to ruleset
- **WHEN** the user clicks a topic name in the unmatched view
- **THEN** the UI SHALL navigate to that topic's ruleset detail view using tvdbId
