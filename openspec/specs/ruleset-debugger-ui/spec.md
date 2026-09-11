## Purpose

Vue.js live debugger panel with Regex101-style rule pipeline trace visualization for testing rulesets against real or manual candidates.

## Requirements

### Requirement: Debugger panel
The right pane of the builder page SHALL display a dual-tab panel with "Search" and "Test Results" tabs instead of the previous "Manual" and "Fetch" tabs. The manual candidate input mode SHALL be removed. Candidates are sourced exclusively from Mediathek search results via the Search tab.

#### Scenario: Panel renders in split pane
- **WHEN** the builder page loads
- **THEN** the search + test panel is visible on the right side with "Search" as the default tab

#### Scenario: Tab switching
- **WHEN** the user clicks the "Test Results" tab
- **THEN** the test results display is shown and the search panel is hidden

### Requirement: Manual candidate input
**This requirement is removed.** Candidates are sourced from Mediathek search results instead of manual entry.

#### Scenario: No manual input form
- **WHEN** the builder page loads
- **THEN** there SHALL be no manual candidate input form

### Requirement: Fetch candidates from MediathekViewWeb
In the Search tab, the panel SHALL display a search field with optional channel and topic filter inputs. Searching SHALL call `GET /api/mediathek/search?q={query}&limit=20` and display the returned candidates as a selectable list. The user SHALL be able to select or deselect individual candidates. A "Select All" toggle SHALL be available.

#### Scenario: Search and display results
- **WHEN** the user types "tatort" and the debounce fires
- **THEN** the API endpoint is called and returned candidates are displayed as a selectable list

#### Scenario: Select candidates
- **WHEN** the user checks 5 of 20 returned candidates
- **THEN** only the 5 selected candidates are included when testing

#### Scenario: Select all
- **WHEN** the user clicks "Select All"
- **THEN** all returned candidates are selected

### Requirement: Test execution
The Search tab SHALL include a "Test Rules" button that sends the current builder state and selected candidates to `POST /api/rulesets/test`. The button SHALL be enabled only when at least one candidate is selected and the builder form has at least a topic and one rule defined. On completion, the panel SHALL switch to the "Test Results" tab.

#### Scenario: Run test
- **WHEN** the user has candidates selected and rules defined and clicks "Test Rules"
- **THEN** the builder form state is serialized, combined with selected candidates, and sent to the test endpoint
- **AND** on response, the panel switches to the Test Results tab

#### Scenario: Test button disabled without candidates
- **WHEN** no candidates are selected
- **THEN** the Test Rules button is disabled

### Requirement: Results display
After a test completes, the Test Results tab SHALL display the results as a list of candidate cards sorted with matched candidates first. Each card SHALL show candidate title, topic, channel, and duration, plus the match result badge.

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
Result candidate cards SHALL use Level 1 card styling (`surface-raised` background with `border-default` borders). Matched cards SHALL have a 4px left border in `status-ok`. Unmatched cards SHALL have a 4px left border in `border-default`. Individual result items within the results list SHALL use Level 2 card styling with hover effects for expand/collapse interaction. Filter condition rows SHALL alternate between `surface-base` and `surface-raised` backgrounds. The rule pipeline trace SHALL use indented sections with `border-l-2` left borders colored by outcome.

#### Scenario: Matched card styling
- **WHEN** a matched candidate card renders
- **THEN** it has a `status-ok` 4px left border, green match badge, and Level 2 hover effect on the header area

#### Scenario: Unmatched card styling
- **WHEN** an unmatched candidate card renders
- **THEN** it has a `border-default` 4px left border and gray badge

#### Scenario: Trace indentation
- **WHEN** a rule pipeline trace is expanded
- **THEN** filter groups are indented under the rule, and conditions are indented under the group

#### Scenario: Result card expand interaction
- **WHEN** the user hovers over a result card header
- **THEN** the header area shows a subtle background change indicating it is clickable

### Requirement: Debugger API client functions
The frontend SHALL expose API client functions: `testRuleSet(config, candidates)` calling `POST /api/rulesets/test` and `searchMediathek(query, limit?)` calling `GET /api/mediathek/search`. Both functions SHALL throw on non-2xx responses.

#### Scenario: Test function sends POST
- **WHEN** `testRuleSet(config, candidates)` is called
- **THEN** a POST request is sent to `/api/rulesets/test` with the serialized body

#### Scenario: Search function sends GET
- **WHEN** `searchMediathek("tatort", 20)` is called
- **THEN** a GET request is sent to `/api/mediathek/search?q=tatort&limit=20`
