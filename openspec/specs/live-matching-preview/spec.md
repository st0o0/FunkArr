# live-matching-preview Specification

## Purpose

Client-side live matching preview for the RuleSet builder: auto-fetches Mediathek candidates based on Topic, runs regex matching in the browser, and displays live match results with an option for full server-side test.

## Requirements

### Requirement: Auto-fetch candidates on topic change
The builder panel SHALL automatically fetch Mediathek candidates when the Topic field changes, debounced by 800ms. Results SHALL be cached in a reactive ref and reused for all subsequent matching until the Topic changes again.

#### Scenario: Topic entered
- **WHEN** the user types "Tatort" in the Topic field and 800ms pass without further typing
- **THEN** `GET /api/mediathek/search?q=Tatort&limit=30` is called and the results are cached

#### Scenario: Topic changed
- **WHEN** the user changes Topic from "Tatort" to "Terra X"
- **THEN** the cached candidates are replaced with new results from the Mediathek search

#### Scenario: Topic cleared
- **WHEN** the user clears the Topic field
- **THEN** the cached candidates are cleared and the preview panel shows an empty state

#### Scenario: Fetch loading state
- **WHEN** the Mediathek search is in flight
- **THEN** the panel shows a loading indicator with "Fetching candidates..."

#### Scenario: Fetch error
- **WHEN** the Mediathek search fails
- **THEN** the panel shows the error message with a "Retry" button

### Requirement: Client-side regex matching
The builder SHALL run client-side regex matching against cached candidates when rules change, debounced by 500ms. Matching SHALL use native JavaScript `RegExp` and extract capture groups.

#### Scenario: Regex pattern matches
- **WHEN** a rule has strategy `seasonAndEpisodeNumber` with `episodeRegex` `(\d+)` and a candidate title is "Folge 42"
- **THEN** the candidate shows as matched with extraction `episode: "42"`

#### Scenario: Regex pattern does not match
- **WHEN** a rule has `episodeRegex` `S(\d+)E(\d+)` and a candidate title is "Tatort: Kommissar"
- **THEN** the candidate shows as not matched

#### Scenario: Title includes matching
- **WHEN** a rule has strategy `itemTitleIncludes` with a static title rule `value: "Tatort"`
- **THEN** candidates whose title contains "Tatort" show as matched

#### Scenario: Multiple rules evaluated
- **WHEN** two rules are defined and a candidate matches the second rule but not the first
- **THEN** the candidate shows as matched by the second rule with its ID displayed

#### Scenario: Live update on pattern change
- **WHEN** the user edits a regex pattern in a rule
- **THEN** match results update within 500ms showing the new match state

### Requirement: Live match results display
The panel SHALL display cached candidates with their live match state. Matched candidates SHALL appear first with a green indicator, extracted values, and the matching rule ID. Unmatched candidates SHALL appear after with a gray indicator.

#### Scenario: Matched candidate display
- **WHEN** a candidate matched rule "airdate" with extraction `airdate: "12.03.2024"`
- **THEN** the candidate card shows a green left border, the rule ID "airdate", and "12.03.2024"

#### Scenario: Unmatched candidate display
- **WHEN** a candidate did not match any rule
- **THEN** the candidate card shows a gray left border and "No Match"

#### Scenario: Candidate count summary
- **WHEN** 8 of 30 candidates match
- **THEN** the panel header shows "8 / 30 matched"

#### Scenario: Empty rules
- **WHEN** no rules are defined in the builder
- **THEN** all candidates show as "No Match" with a hint "Add rules to see matches"

### Requirement: Manual refresh
The panel SHALL provide a Refresh button to re-fetch Mediathek candidates without changing the Topic.

#### Scenario: Click refresh
- **WHEN** the user clicks the Refresh button
- **THEN** the Mediathek search is re-executed with the current Topic and results are updated

### Requirement: Full test button
The panel SHALL provide a "Full Test" button that sends all cached candidates and the current builder state to `POST /api/rulesets/test` for server-side pipeline evaluation.

#### Scenario: Run full test
- **WHEN** the user clicks "Full Test"
- **THEN** the builder state and all cached candidates are sent to the test endpoint
- **AND** the results replace the live preview with full pipeline results including filter evaluation and scoring trace

#### Scenario: Full test loading state
- **WHEN** the full test request is in flight
- **THEN** the button shows a loading indicator

#### Scenario: Expand full test trace
- **WHEN** full test results are displayed and the user clicks a candidate card
- **THEN** the full rule pipeline trace expands showing filter conditions, identification details, and scoring

### Requirement: Client-side matching composable
The client-side matching logic SHALL be extracted into a Vue composable `useRulesetMatcher` that accepts reactive candidate and rule refs and returns reactive match results.

#### Scenario: Composable reactivity
- **WHEN** the rules ref changes
- **THEN** the composable recomputes match results after 500ms debounce

#### Scenario: Composable output
- **WHEN** matching completes
- **THEN** the composable returns `matchedCount`, `total`, and an array of results with `candidate`, `matchedRuleId`, and `extractions`
