## Purpose

UI views for browsing match records, per-topic match statistics, and unmatched items from the MatchLedger.

## Requirements

### Requirement: Recent matches view
The UI SHALL display recent match records from the MatchLedger, showing search topic, timestamp, matched/filtered/unmatched counts, and expandable details.

#### Scenario: Show recent matches
- **WHEN** the user navigates to the matches view
- **THEN** the UI SHALL display the most recent match records with topic, timestamp, and result counts

#### Scenario: Expand match record
- **WHEN** the user expands a match record
- **THEN** the UI SHALL display the individual matched traces (rule index, strategy, season, episode) and unmatched traces (failure reasons)

### Requirement: Topic stats view
The UI SHALL display per-topic match statistics: search count, items evaluated, matched/filtered/unmatched counts, match rate, and per-rule hit counts.

#### Scenario: Show all topic stats
- **WHEN** the user views topic stats
- **THEN** the UI SHALL display all topics sorted by match rate ascending (worst first) with their statistics

#### Scenario: Low match rate highlighting
- **WHEN** a topic has a match rate below 75%
- **THEN** the UI SHALL highlight it as needing attention

### Requirement: Unmatched items explorer
The UI SHALL display unmatched items grouped by topic, with each item showing the title, duration, channel, and per-rule failure reasons.

#### Scenario: Browse unmatched items
- **WHEN** the user views unmatched items
- **THEN** the UI SHALL display unmatched items grouped by topic, sorted by group size descending

#### Scenario: Filter by topic
- **WHEN** the user selects a specific topic
- **THEN** only unmatched items for that topic SHALL be displayed

#### Scenario: Navigate to ruleset
- **WHEN** the user clicks a topic name in the unmatched view
- **THEN** the UI SHALL navigate to that topic's ruleset detail view
