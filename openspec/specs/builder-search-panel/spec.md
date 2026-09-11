# builder-search-panel Specification

## Purpose

Dual-tab right pane for the RuleSet builder providing Mediathek search with candidate selection and test result display, replacing the previous manual/fetch debugger tabs.

## Requirements

### Requirement: Builder right pane dual tabs

The RuleSetBuilder right pane SHALL display two tabs: "Search" and "Test Results". The Search tab SHALL be the default. The existing DebuggerPanel's manual candidate input SHALL be removed; candidates are sourced exclusively from Mediathek search results.

#### Scenario: Default tab on mount
- **WHEN** the RuleSetBuilder page loads
- **THEN** the right pane SHALL display the Search tab

#### Scenario: Tab switching
- **WHEN** the user clicks "Test Results"
- **THEN** the Test Results panel SHALL render and the Search panel SHALL be hidden

### Requirement: Search tab Mediathek search

The Search tab SHALL display a search input field and optional filter inputs for channel and topic. Typing in the search input SHALL trigger a debounced (400ms) API call to `GET /api/mediathek/search`. Results SHALL display as a scrollable list of candidate cards.

#### Scenario: Search for candidates
- **WHEN** the user types "tatort" in the search input
- **THEN** after 400ms the system SHALL call `GET /api/mediathek/search?q=tatort&limit=20`

#### Scenario: Search with filters
- **WHEN** the user types "tatort" with channel filter "ARD"
- **THEN** the API call SHALL include `channel=ARD`

#### Scenario: Search results display
- **WHEN** the API returns 15 candidates
- **THEN** each candidate SHALL display title, topic, channel, duration, and quality

#### Scenario: No results
- **WHEN** the API returns zero results
- **THEN** the panel SHALL show "No results found"

#### Scenario: Search error
- **WHEN** the API returns an error
- **THEN** the panel SHALL show the error message

### Requirement: Candidate selection

Each search result card SHALL have a checkbox to select it as a test candidate. A "Select All" toggle SHALL be available above the results list. Selected candidate count SHALL be displayed.

#### Scenario: Select individual candidate
- **WHEN** the user checks a candidate's checkbox
- **THEN** the candidate is added to the test candidate set

#### Scenario: Deselect candidate
- **WHEN** the user unchecks a selected candidate's checkbox
- **THEN** the candidate is removed from the test candidate set

#### Scenario: Select all
- **WHEN** the user clicks "Select All"
- **THEN** all displayed search results SHALL be selected

#### Scenario: Selected count display
- **WHEN** the user has selected 5 of 15 results
- **THEN** the panel SHALL display "5 selected"

### Requirement: Test execution from search

The Search tab SHALL include a "Test Rules" button that sends the current builder form state and selected candidates to `POST /api/rulesets/test`. The button SHALL be disabled when no candidates are selected or no rules are defined. On completion, the view SHALL automatically switch to the Test Results tab.

#### Scenario: Run test
- **WHEN** the user has selected 5 candidates and has rules defined and clicks "Test Rules"
- **THEN** the builder form state SHALL be serialized as a matching config, combined with the selected candidates, and sent to `POST /api/rulesets/test`
- **AND** after the response arrives, the view SHALL switch to the Test Results tab

#### Scenario: Test button disabled without candidates
- **WHEN** no candidates are selected
- **THEN** the "Test Rules" button SHALL be disabled

#### Scenario: Test button disabled without rules
- **WHEN** no rules are defined in the builder form
- **THEN** the "Test Rules" button SHALL be disabled

#### Scenario: Loading state during test
- **WHEN** the test request is in flight
- **THEN** the "Test Rules" button SHALL show a loading indicator and be non-clickable

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
