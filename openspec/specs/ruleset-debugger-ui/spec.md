## Purpose

Vue.js live debugger panel with Regex101-style rule pipeline trace visualization for testing rulesets against real or manual candidates.

## Requirements

### Requirement: Debugger panel
The debugger panel SHALL occupy the right pane of the builder page's split-pane layout. It SHALL display test candidates and their scoring results. The panel SHALL have two tabs for input mode selection: "Manual" and "Fetch".

#### Scenario: Panel renders in split pane
- **WHEN** the builder page loads
- **THEN** the debugger panel is visible on the right side

#### Scenario: Tab switching
- **WHEN** the user clicks the "Fetch" tab
- **THEN** the fetch input mode is displayed and the manual form is hidden

### Requirement: Manual candidate input
In manual input mode, the debugger panel SHALL display a form for entering a single test candidate with fields: `title` (text input), `topic` (text input), `channel` (text input), `duration` (number input, minutes, converted to seconds for API), `quality` (number input), `description` (textarea, optional), and `timestamp` (datetime-local input, converted to unix seconds for API). The user SHALL be able to add multiple manual candidates to a list.

#### Scenario: Enter manual candidate
- **WHEN** the user fills in title "Tatort: Fangschuss (S01/E1234)", topic "Tatort", channel "Das Erste", duration 90
- **THEN** the candidate appears in the test candidate list

#### Scenario: Add multiple candidates
- **WHEN** the user adds 3 manual candidates
- **THEN** all 3 appear in the candidate list

#### Scenario: Remove candidate
- **WHEN** the user clicks remove on a candidate
- **THEN** it is removed from the list

#### Scenario: Duration conversion
- **WHEN** the user enters 90 in the duration field (minutes)
- **THEN** the value is sent to the API as 5400 (seconds)

### Requirement: Fetch candidates from MediathekViewWeb
In fetch input mode, the debugger panel SHALL display a search field and a "Search" button. Clicking search SHALL call `GET /api/mediathek/search?q={query}&limit=20` and display the returned candidates in a selectable list. The user SHALL be able to select or deselect individual candidates to include in testing. A "Select All" toggle SHALL be available.

#### Scenario: Search and display results
- **WHEN** the user types "tatort" and clicks Search
- **THEN** the proxy endpoint is called and returned candidates are displayed as a selectable list

#### Scenario: Select candidates
- **WHEN** the user checks 5 of 20 returned candidates
- **THEN** only the 5 selected candidates are included in the test candidate list

#### Scenario: Select all
- **WHEN** the user clicks "Select All"
- **THEN** all returned candidates are selected

#### Scenario: Search with no results
- **WHEN** MediathekViewWeb returns no results
- **THEN** the panel shows "No results found"

#### Scenario: Search error
- **WHEN** the proxy endpoint returns an error
- **THEN** the panel shows the error message

### Requirement: Test execution
The debugger panel SHALL include a "Test" button that sends the current builder state and selected candidates to `POST /api/rulesets/test`. The button SHALL be enabled only when at least one candidate is in the list and the builder form has at least a topic and one rule defined. While the request is in flight, the button SHALL show a loading state.

#### Scenario: Run test
- **WHEN** the user has candidates and rules defined and clicks "Test"
- **THEN** the builder form state is serialized as a matching config, combined with the candidate list, and sent to the test endpoint

#### Scenario: Test button disabled without candidates
- **WHEN** no candidates are in the list
- **THEN** the Test button is disabled

#### Scenario: Test button disabled without rules
- **WHEN** no rules are defined in the builder
- **THEN** the Test button is disabled

#### Scenario: Loading state during test
- **WHEN** the test request is in flight
- **THEN** the Test button shows a spinner and is not clickable

### Requirement: Results display
After a test completes, the debugger panel SHALL display the results as a list of candidate cards. Each card SHALL show the candidate title, topic, channel, and duration, plus the match result: a green badge with "Matched" and the matched rule ID for matched candidates, or a gray badge with "No Match" for unmatched ones. Matched candidates SHALL appear first, followed by unmatched.

#### Scenario: Display matched candidate
- **WHEN** a candidate matched rule "regex-se" with score 0.95
- **THEN** the card shows a green "Matched" badge, rule ID "regex-se", and score 0.95

#### Scenario: Display unmatched candidate
- **WHEN** a candidate did not match any rule
- **THEN** the card shows a gray "No Match" badge

#### Scenario: Sort order
- **WHEN** 3 of 10 candidates matched
- **THEN** the 3 matched candidates appear first, followed by 7 unmatched

### Requirement: Rule pipeline trace
Each result candidate card SHALL be expandable to show the full rule pipeline trace. The expanded view SHALL show each rule evaluated in priority order. For each rule, the trace SHALL display: rule ID, priority, and outcome (Matched, FilterFailed, IdentificationFailed) with color-coded badges (green for Matched, red for FilterFailed, amber for IdentificationFailed, gray for skipped).

#### Scenario: Expand trace for matched candidate
- **WHEN** the user expands a matched candidate's trace
- **THEN** rules are shown in priority order, with the first matching rule showing a green "Matched" badge and subsequent rules marked as "Skipped" in gray

#### Scenario: Expand trace for unmatched candidate
- **WHEN** the user expands an unmatched candidate's trace
- **THEN** all rules show their outcome: red for FilterFailed, amber for IdentificationFailed

### Requirement: Filter trace detail
Within an expanded rule trace, the filter section SHALL show each filter group (ALL, ANY, NOT) with its conditions. Each condition SHALL display: field name, operator, expected value, actual value from the candidate, and a pass/fail indicator. Skipped conditions (short-circuit evaluation) SHALL be shown in gray with a "Skipped" label.

#### Scenario: Filter condition passed
- **WHEN** a filter condition `duration > 60` evaluated against a candidate with duration 90
- **THEN** the condition shows field "duration", op ">", value "60", actual "90", and a green check

#### Scenario: Filter condition failed
- **WHEN** a filter condition `channel eq "ARD"` evaluated against a candidate with channel "ZDF"
- **THEN** the condition shows field "channel", op "eq", value "ARD", actual "ZDF", and a red cross

#### Scenario: Skipped condition
- **WHEN** a condition in an ALL group was not evaluated because an earlier condition failed
- **THEN** the condition shows in gray with "Skipped"

#### Scenario: ANY group partial evaluation
- **WHEN** an ANY group has 3 conditions and the first one passes
- **THEN** the first condition shows a green check, remaining conditions show "Skipped"

### Requirement: Identification trace detail
Within an expanded rule trace, the identification section SHALL show: the strategy name, whether identification was attempted, extracted values (season, episode, or constructed title), and the failure reason if identification failed.

#### Scenario: Successful RegexCapture identification
- **WHEN** RegexCapture extracted season "01" and episode "1234"
- **THEN** the trace shows strategy "RegexCapture", season "01", episode "1234"

#### Scenario: Failed RegexCapture identification
- **WHEN** RegexCapture failed because "episode pattern did not match"
- **THEN** the trace shows strategy "RegexCapture" with the failure reason

#### Scenario: Successful TitleConstruction identification
- **WHEN** TitleConstruction produced title "Fangschuss" and it matched
- **THEN** the trace shows strategy "TitleConstruction" and the constructed title

#### Scenario: Identification not attempted
- **WHEN** a rule's filters failed before identification
- **THEN** the identification section shows "Not attempted" (filters failed first)

### Requirement: Debugger presentation
Result candidate cards SHALL use `surface-raised` background with `border-default` borders. Matched cards SHALL have a left border in `status-ok`. Unmatched cards SHALL have a left border in `border-default`. Filter condition rows SHALL alternate between `surface-base` and `surface-raised` backgrounds. The rule pipeline trace SHALL use indented sections with `border-l-2` left borders colored by outcome.

#### Scenario: Matched card styling
- **WHEN** a matched candidate card renders
- **THEN** it has a `status-ok` left border and green match badge

#### Scenario: Unmatched card styling
- **WHEN** an unmatched candidate card renders
- **THEN** it has a `border-default` left border and gray badge

#### Scenario: Trace indentation
- **WHEN** a rule pipeline trace is expanded
- **THEN** filter groups are indented under the rule, and conditions are indented under the group

### Requirement: Debugger API client functions
The frontend SHALL expose API client functions: `testRuleSet(config, candidates)` calling `POST /api/rulesets/test` and `searchMediathek(query, limit?)` calling `GET /api/mediathek/search`. Both functions SHALL throw on non-2xx responses.

#### Scenario: Test function sends POST
- **WHEN** `testRuleSet(config, candidates)` is called
- **THEN** a POST request is sent to `/api/rulesets/test` with the serialized body

#### Scenario: Search function sends GET
- **WHEN** `searchMediathek("tatort", 20)` is called
- **THEN** a GET request is sent to `/api/mediathek/search?q=tatort&limit=20`
