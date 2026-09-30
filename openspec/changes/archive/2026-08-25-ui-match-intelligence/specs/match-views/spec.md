## MODIFIED Requirements

### Requirement: Recent matches view
The UI SHALL display recent match records from the MatchLedger, showing search topic, timestamp, matched/filtered/unmatched counts. Each record SHALL link to the match detail view at `/matches/:id` instead of expanding inline.

#### Scenario: Show recent matches
- **WHEN** the user navigates to the matches view
- **THEN** the UI SHALL display the most recent match records with topic, timestamp, and result counts

#### Scenario: Navigate to match detail
- **WHEN** the user clicks a match record
- **THEN** the UI SHALL navigate to `/matches/:id` to show the full trace visualization

## ADDED Requirements

### Requirement: Per-rule hit stats on ruleset detail
The ruleset detail view SHALL display per-rule match statistics from the `TopicStats.perRuleHitCounts` data on each rule card.

#### Scenario: Show per-rule stats
- **WHEN** the ruleset detail view loads and topic stats are available
- **THEN** each rule card SHALL display a mini progress bar showing the number of matches that rule produced and its percentage of total matches

#### Scenario: No stats available
- **WHEN** the ruleset detail view loads but no topic stats exist (no searches have been performed yet)
- **THEN** each rule card SHALL display "No match data yet" in muted text

#### Scenario: Stats fetch
- **WHEN** the ruleset detail view loads for a ruleset with a `tvdbId`
- **THEN** the view SHALL fetch topic stats from `GET /api/v1/matches/topics/{tvdbId}`
