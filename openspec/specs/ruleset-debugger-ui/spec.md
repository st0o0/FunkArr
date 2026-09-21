## Purpose

Vue.js live debugger panel with Regex101-style rule pipeline trace visualization for testing rulesets against real or manual candidates.

## Requirements

### Requirement: Debugger panel
The right pane of the builder page SHALL display a live preview panel instead of the previous dual-tab panel. The panel SHALL show auto-fetched candidates with live client-side match results by default. When the user runs a Full Test, the panel SHALL switch to showing server-side pipeline results with the existing trace visualization. All labels, headings, and status text SHALL use `$t()` translation calls.

#### Scenario: Panel renders live preview
- **WHEN** the builder page loads and Topic is set
- **THEN** the panel shows fetched candidates with live match indicators

#### Scenario: Panel switches to full test results
- **WHEN** the user clicks "Full Test" and results return
- **THEN** the panel shows server-side results with expandable rule pipeline traces

#### Scenario: Return to live preview
- **WHEN** the user edits a rule after viewing full test results
- **THEN** the panel returns to live preview mode with updated client-side matches

#### Scenario: Translated labels in German
- **WHEN** the locale is `de` and the debugger panel renders
- **THEN** labels display German translations (e.g., "Vollständiger Test" instead of "Full Test", "Live-Vorschau" instead of "Live Preview")

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
After a full test completes, the panel SHALL display results as a list of candidate cards sorted with matched candidates first. Each card SHALL show candidate title, topic, channel, and duration, plus the match result badge. Badge labels ("Matched", "No Match") SHALL use translation keys. This requirement is unchanged from the current behavior but now applies only to full test results, not to the live preview.

#### Scenario: Display matched candidate
- **WHEN** a full test candidate matched rule "regex-se" with score 0.95
- **THEN** the card shows a green "Matched" badge, rule ID "regex-se", and score 0.95

#### Scenario: Display unmatched candidate
- **WHEN** a full test candidate did not match any rule
- **THEN** the card shows a gray "No Match" badge

#### Scenario: Sort order
- **WHEN** 3 of 10 candidates matched in full test
- **THEN** the 3 matched candidates appear first, followed by 7 unmatched

#### Scenario: German badge labels
- **WHEN** the locale is `de`
- **THEN** badges read "Treffer" and "Kein Treffer" instead of "Matched" and "No Match"

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

### Requirement: Filter trace rendering
The `FilterGroupTraceView` component SHALL use the shared `opSymbol()` function to display compact operator symbols instead of raw enum names in filter condition traces. The component SHALL use the shared `groupLabel()` function to display localized group headers instead of the raw `group.operator` string from the API.

#### Scenario: Op symbols in trace
- **WHEN** a filter trace shows a condition with op "greaterThan"
- **THEN** the condition displays `>` instead of `greaterThan`

#### Scenario: Localized group header in trace
- **WHEN** a filter trace group has operator "all" and locale is DE
- **THEN** the group header displays the localized label instead of raw "all"

#### Scenario: Field labels in trace
- **WHEN** a filter trace shows a condition with field "duration"
- **THEN** the condition displays the localized field label instead of raw "duration"

#### Scenario: Trace coloring preserved
- **WHEN** filter trace conditions render with shared vocabulary
- **THEN** pass/fail coloring and skip states SHALL be preserved unchanged

### Requirement: Enrichment config in RuleSet Detail view
The RuleSetDetail view SHALL display enrichment config in a read-only section, showing enabled/disabled status, active methods, and key threshold values.

#### Scenario: Detail view shows enrichment config
- **WHEN** user views a ruleset detail page
- **THEN** an enrichment section shows: enabled/disabled badge, active methods as tags, and threshold values for active methods

#### Scenario: Detail view with enrichment disabled
- **WHEN** a ruleset has enrichment disabled
- **THEN** the enrichment section shows a "disabled" badge and no method details

### Requirement: Enrichment results in full test mode
The LiveMatchPreview full test mode SHALL display enrichment results for matched items when enrichment data is present in the test response.

#### Scenario: Show enrichment trace for resolved item
- **WHEN** a matched item has an EnrichmentTrace with enriched=true
- **THEN** the item's expanded view shows: resolved title, season/episode, match method, and confidence below the rule pipeline

#### Scenario: Show unresolved indicator
- **WHEN** a matched item has an EnrichmentTrace with enriched=false
- **THEN** the item shows an amber "not enriched" indicator

#### Scenario: No enrichment data
- **WHEN** a matched item has null EnrichmentTrace
- **THEN** no enrichment section is shown

### Requirement: Test request includes enrichment from builder
The LiveMatchPreview full test SHALL include the builder's current enrichment config and identity fields in the test API request.

#### Scenario: Full test sends enrichment config
- **WHEN** user clicks "Full Test" with enrichment enabled and tvdbId set
- **THEN** the POST /api/rulesets/test request includes enrichment config, tvdbId, and mediaType

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

### Requirement: Live preview mode indicator
The panel SHALL clearly indicate whether it is showing live client-side preview results or full server-side test results. The indicator text SHALL use translation keys.

#### Scenario: Live preview mode
- **WHEN** the panel shows client-side match results
- **THEN** a subtle label "Live Preview" is visible and a disclaimer "Run Full Test for accurate scoring" is shown

#### Scenario: Full test mode
- **WHEN** the panel shows server-side test results
- **THEN** a label "Full Test Results" is visible with no disclaimer
