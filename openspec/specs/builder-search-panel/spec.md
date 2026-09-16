# builder-search-panel Specification

## Purpose

Live preview right pane for the RuleSet builder providing topic-driven auto-fetch of Mediathek candidates with live match results and on-demand full server-side test.

## Requirements

### Requirement: Builder right pane dual tabs

The RuleSetBuilder right pane SHALL display a single live preview panel instead of the previous Search and Test Results tabs. The panel SHALL show auto-fetched Mediathek candidates with live match results. A "Full Test" button SHALL be available for server-side pipeline validation.

#### Scenario: Default state on mount
- **WHEN** the RuleSetBuilder page loads
- **THEN** the right pane SHALL display the live preview panel with an empty state prompting the user to enter a Topic

#### Scenario: Auto-fetch replaces manual search
- **WHEN** the user enters a Topic
- **THEN** candidates are fetched automatically without the user needing to type a search query or click a search button

### Requirement: Search tab Mediathek search

The manual search input, channel/topic filters, and explicit search button are replaced by topic-driven auto-fetch. The search query is derived from the Topic field in the builder form. Results are cached and displayed as a live match list instead of a selectable candidate list.

#### Scenario: Topic-driven fetch
- **WHEN** the user sets Topic to "Tatort" in the identity section
- **THEN** after 800ms the system SHALL call `GET /api/mediathek/search?q=Tatort&limit=30`

#### Scenario: Results displayed as match list
- **WHEN** candidates are fetched
- **THEN** each candidate SHALL display title, topic, channel, duration, and its live match state against current rules

### Requirement: Candidate selection

Candidate selection checkboxes and "Select All" toggle are removed. All fetched candidates are automatically used for matching.

#### Scenario: All candidates used
- **WHEN** 30 candidates are fetched
- **THEN** all 30 are matched against the current rules without manual selection

### Requirement: Test Results tab display

The Test Results tab SHALL display test results as a list of candidate cards sorted with matched candidates first. Each card SHALL show candidate title, topic, channel, duration, and match result: a green badge with "Matched" and matched rule ID for matched candidates, or a gray badge with "No Match" for unmatched ones.

#### Scenario: Matched candidate display
- **WHEN** a candidate matched rule "se-number" with score 0.90
- **THEN** the card SHALL show a green "Matched" badge, rule ID "se-number", and score 0.90

#### Scenario: Unmatched candidate display
- **WHEN** a candidate did not match any rule
- **THEN** the card SHALL show a gray "No Match" badge

#### Scenario: Sort order
- **WHEN** 3 of 10 candidates matched
- **THEN** the 3 matched candidates SHALL appear first, followed by 7 unmatched

### Requirement: Test Results rule trace expansion

Each Test Results candidate card SHALL be expandable to show the full rule pipeline trace. The expanded view SHALL show each rule evaluated in priority order with outcome badges and filter/identification trace details. This SHALL reuse the existing trace visualization from the current DebuggerPanel.

#### Scenario: Expand matched trace
- **WHEN** the user expands a matched candidate's trace
- **THEN** rules SHALL show in priority order with the matching rule highlighted green and subsequent rules as "Skipped"

#### Scenario: Expand unmatched trace
- **WHEN** the user expands an unmatched candidate's trace
- **THEN** all rules SHALL show their failure outcome (FilterFailed or IdentificationFailed) with details

#### Scenario: Filter condition detail
- **WHEN** a rule trace is expanded showing filter evaluation
- **THEN** each condition SHALL display field, operator, expected value, actual value, and pass/fail indicator
