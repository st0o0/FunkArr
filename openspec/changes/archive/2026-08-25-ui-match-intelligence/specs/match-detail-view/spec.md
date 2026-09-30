## ADDED Requirements

### Requirement: Match detail route
The app SHALL provide a route at `/matches/:id` that displays a single MatchRecord with full trace visualization.

#### Scenario: Navigate to match detail
- **WHEN** the user clicks a match record in the Recent list
- **THEN** the app SHALL navigate to `/matches/:id` where `:id` is the record's ID

#### Scenario: Record found
- **WHEN** the match detail view loads and the record ID exists in the recent matches
- **THEN** the view SHALL display the full match evaluation story

#### Scenario: Record not found
- **WHEN** the match detail view loads but the record ID is not in the recent matches (evicted from the 100-record window)
- **THEN** the view SHALL display "Record no longer available" with a link back to `/matches`

### Requirement: Match detail header
The match detail view SHALL display a header with the search topic, season/episode (for TV), timestamp, source badge, and total results count.

#### Scenario: TV search header
- **WHEN** the match record has a `tvdbId`, `season`, and `episode`
- **THEN** the header SHALL display the topic, formatted season/episode (e.g. "S02E05"), and source badge

#### Scenario: Movie search header
- **WHEN** the match record has no `tvdbId`
- **THEN** the header SHALL display the topic and source badge without season/episode

### Requirement: Match detail summary bar
The match detail view SHALL display a summary bar with three stat boxes showing matched, filtered, and unmatched counts.

#### Scenario: Summary counts
- **WHEN** the match record is displayed
- **THEN** the summary bar SHALL show matched count in green, filtered count in amber, and unmatched count in red

### Requirement: Matched items section
The matched items section SHALL display each matched trace with human-readable details.

#### Scenario: Display matched trace
- **WHEN** a matched trace is shown
- **THEN** it SHALL display: item title, rule index, strategy name, confidence as a colored badge (green ≥0.8, amber ≥0.5, red <0.5), and resolved episode name

### Requirement: Filtered items section
The filtered items section SHALL display each filtered trace with the filter explanation.

#### Scenario: Display filtered trace
- **WHEN** a filtered trace is shown
- **THEN** it SHALL display: item title, human-readable reason label, and the filter details (field, operator, expected value vs. actual value)

#### Scenario: Accessibility skip label
- **WHEN** a filtered trace has `reason = "accessibility-skip"`
- **THEN** the reason SHALL be displayed as "Accessibility variant skipped"

### Requirement: Unmatched items section
The unmatched items section SHALL display each unmatched trace with a per-rule failure pipeline.

#### Scenario: Display unmatched pipeline
- **WHEN** an unmatched trace is shown
- **THEN** it SHALL display the item title followed by a vertical step list of rule failures, each showing: rule index, failure icon, failure reason, and detail string

#### Scenario: Filter failure detail
- **WHEN** a rule failure has `failReason = "filter-failed"`
- **THEN** the step SHALL display the filter condition from the `detail` field

#### Scenario: Strategy failure detail
- **WHEN** a rule failure has `failReason = "strategy-no-match"`
- **THEN** the step SHALL display the strategy name from the `detail` field

#### Scenario: Collapse long unmatched lists
- **WHEN** there are more than 5 unmatched items
- **THEN** the unmatched section SHALL be collapsed by default with a count indicator

### Requirement: Ruleset link
The match detail view SHALL provide a link to the ruleset that produced the evaluation.

#### Scenario: Navigate to ruleset
- **WHEN** the user clicks the ruleset link in the match detail footer
- **THEN** the app SHALL navigate to `/rulesets/:topic`
