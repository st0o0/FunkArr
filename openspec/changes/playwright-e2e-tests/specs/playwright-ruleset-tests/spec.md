# playwright-ruleset-tests Specification

## Purpose

Tests for RuleSet views: list search/filter/sort, detail sections with export/delete, builder form validation and save, scoring history pagination, and scoring detail traces.

## Requirements

### Requirement: RuleSet list rendering and filtering

The list page SHALL render ruleset cards and support search, type, and source filtering.

#### Scenario: List renders cards
- **WHEN** the API returns 5 rulesets
- **THEN** 5 cards SHALL be visible with topic, media type, and metadata

#### Scenario: Search filters by topic
- **WHEN** the user types "tatort" in the search input
- **THEN** only rulesets with "tatort" in their topic or aliases SHALL be visible

#### Scenario: Type filter tabs
- **WHEN** the user clicks the "Movies" tab
- **THEN** only movie rulesets SHALL be visible

#### Scenario: Source tabs hidden with single source
- **WHEN** all rulesets are community-sourced
- **THEN** source filter tabs SHALL NOT be visible

#### Scenario: Empty list
- **WHEN** the API returns 0 rulesets
- **THEN** an empty state with "Create first" button SHALL be visible

#### Scenario: Loading skeleton
- **WHEN** the API has not yet responded
- **THEN** 3 skeleton cards SHALL be visible

#### Scenario: Click card navigates
- **WHEN** the user clicks a ruleset card
- **THEN** the browser SHALL navigate to `/rulesets/{id}`

### Requirement: RuleSet detail sections

The detail page SHALL show identity, rules, and enrichment sections with appropriate actions.

#### Scenario: Identity section
- **WHEN** viewing a ruleset detail
- **THEN** topic, media type, aliases, and external IDs SHALL be visible

#### Scenario: Rules section with expandable rows
- **WHEN** the ruleset has 3 rules
- **THEN** 3 rule rows SHALL be visible and the first SHALL be expanded by default

#### Scenario: Enrichment section
- **WHEN** the ruleset has enrichment config
- **THEN** the enrichment section SHALL show methods and thresholds

#### Scenario: No enrichment
- **WHEN** the ruleset has no enrichment config
- **THEN** the enrichment section SHALL NOT be visible

#### Scenario: Export button for local rulesets
- **WHEN** the ruleset has a local source path
- **THEN** the "Export for Community" button SHALL be visible

#### Scenario: Delete with confirmation
- **WHEN** the user clicks "Delete Local" and confirms
- **THEN** the system SHALL call `DELETE /api/rulesets/{id}` and show a success toast

#### Scenario: Delete cancelled
- **WHEN** the user clicks "Delete Local" then "Cancel"
- **THEN** no API call SHALL be made and the dialog SHALL close

### Requirement: RuleSet builder form

The builder SHALL support creating and editing rulesets with validation feedback.

#### Scenario: Create new ruleset
- **WHEN** the user fills in topic, adds a rule, and clicks Save
- **THEN** the system SHALL call `POST /api/rulesets` and navigate to the new detail page

#### Scenario: Edit existing ruleset
- **WHEN** navigating to `/rulesets/{id}/edit`
- **THEN** the form SHALL be pre-filled with the existing ruleset data

#### Scenario: Validation errors
- **WHEN** the user clicks Save with an empty topic
- **THEN** a validation error banner SHALL be visible with field-level messages

#### Scenario: Live match preview
- **WHEN** the user types a search query in the preview panel
- **THEN** the system SHALL call `POST /api/rulesets/test` and show matched results

### Requirement: Scoring history

The scoring history page SHALL show paginated scoring snapshots.

#### Scenario: History table renders
- **WHEN** the API returns scoring snapshots
- **THEN** the table SHALL show source, query, timestamp, candidates, and matched count

#### Scenario: Pagination controls
- **WHEN** there are more than 20 snapshots
- **THEN** Previous and Next buttons SHALL be visible

#### Scenario: Row navigates to detail
- **WHEN** the user clicks a history row
- **THEN** the browser SHALL navigate to `/rulesets/{id}/history/{requestId}`

### Requirement: Scoring detail

The scoring detail page SHALL show filter tabs and expandable trace cards.

#### Scenario: Filter tabs
- **WHEN** the detail page loads with matched and unmatched items
- **THEN** filter tabs (All, Matched, Unmatched) SHALL be visible with correct counts

#### Scenario: Expand trace card
- **WHEN** the user clicks a candidate card
- **THEN** the rule traces and enrichment traces SHALL expand below
