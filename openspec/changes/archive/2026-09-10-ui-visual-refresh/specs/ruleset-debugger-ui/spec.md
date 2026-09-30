## MODIFIED Requirements

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
