## MODIFIED Requirements

### Requirement: Match detail route
The app SHALL provide a route at `/matches/:id` that displays a single SearchEvaluation with full item evaluation visualization.

#### Scenario: Navigate to match detail
- **WHEN** the user clicks a match record in the Recent list
- **THEN** the app SHALL navigate to `/matches/:id` where `:id` is the record's ID

#### Scenario: Record found
- **WHEN** the match detail view loads and the record ID exists in the recent search evaluations
- **THEN** the view SHALL display the full evaluation story with expandable pipeline rows

#### Scenario: Record not found
- **WHEN** the match detail view loads but the record ID is not in the recent search evaluations (evicted from the ring buffer)
- **THEN** the view SHALL display "Record no longer available" with a link back to `/matches`

### Requirement: Match detail header
The match detail view SHALL display a header with the search topic, season/episode (for TV), timestamp, source badge, and total results count.

#### Scenario: TV search header
- **WHEN** the search evaluation has a `tvdbId`, `season`, and `episode`
- **THEN** the header SHALL display the topic, formatted season/episode (e.g. "S02E05"), and source badge

#### Scenario: Movie search header
- **WHEN** the search evaluation has no `tvdbId`
- **THEN** the header SHALL display the topic and source badge without season/episode

### Requirement: Match detail summary bar
The match detail view SHALL display a summary bar with three stat boxes showing matched, filtered, and unmatched counts derived from the items array by outcome.

#### Scenario: Summary counts
- **WHEN** the search evaluation is displayed
- **THEN** the summary bar SHALL show matched count (items with outcome=0) in green, filtered count (outcome=1) in amber, and unmatched count (outcome=2) in red

### Requirement: Item rows with raw data
Each item in the matched, filtered, and unmatched sections SHALL display the raw Mediathek item data: title, topic, channel, and duration (formatted as minutes).

#### Scenario: Raw data visible on compact row
- **WHEN** an item evaluation is rendered in its default (collapsed) state
- **THEN** the row SHALL show the item title prominently and topic, channel, duration as secondary metadata

### Requirement: Expandable evaluation pipeline
Each matched and unmatched item SHALL be expandable to show the full `RuleEvaluation[]` pipeline as a vertical stepper.

#### Scenario: Expand matched item
- **WHEN** the user clicks a matched item row
- **THEN** the row SHALL expand to show all RuleEvaluation entries in priority order, each showing: rule index, priority, strategy name, outcome badge, filter checks with pass/fail indicators, and strategy detail

#### Scenario: Expand unmatched item
- **WHEN** the user clicks an unmatched item row
- **THEN** the row SHALL expand to show all RuleEvaluation entries, each showing why that rule failed (filter or strategy)

#### Scenario: Filtered items not expandable
- **WHEN** a filtered item (outcome=1) is displayed
- **THEN** it SHALL show the filter reason but SHALL NOT be expandable (no rule pipeline exists)

### Requirement: Filter check visualization
Within an expanded RuleEvaluation, each FilterCheck SHALL be displayed with field, operator, expected value, actual value, and a pass/fail indicator.

#### Scenario: Passing filter check
- **WHEN** a FilterCheck has passed=true, field="duration", op=GreaterThan, value="35", actual="49"
- **THEN** it SHALL render as: `duration > 35 ✓ (actual: 49min)` with green indicator

#### Scenario: Failing filter check
- **WHEN** a FilterCheck has passed=false, field="duration", op=GreaterThan, value="35", actual="12"
- **THEN** it SHALL render as: `duration > 35 ✗ (actual: 12min)` with red indicator

### Requirement: Strategy detail visualization
Within an expanded RuleEvaluation where StrategyDetail is present, the view SHALL show the regex pattern, input, match result, captured value, constructed title, and TVDB match as applicable.

#### Scenario: Regex with successful capture
- **WHEN** StrategyDetail has regexPattern=`\((\d+)\)`, regexInput="Sturm der Liebe (1622)", regexMatched=true, capturedValue="1622"
- **THEN** it SHALL render showing the pattern, the input string, and the captured value highlighted

#### Scenario: Regex with no match
- **WHEN** StrategyDetail has regexPattern=`Episode\s(\d+)`, regexInput="Sturm der Liebe (1622)", regexMatched=false
- **THEN** it SHALL render showing the pattern and input with a "no match" indicator

#### Scenario: TVDB lookup result
- **WHEN** StrategyDetail has tvdbMatch="Folge 1622"
- **THEN** it SHALL render showing the TVDB episode name as the lookup result

### Requirement: Matched items section
The matched items section SHALL group all items with outcome=Matched (0) and display them with their winning rule info on the compact row.

#### Scenario: Display matched compact row
- **WHEN** a matched item is shown in collapsed state
- **THEN** it SHALL display: item title, topic/channel/duration, rule index, strategy name, confidence badge (green >=0.8, amber >=0.5, red <0.5), and resolved season/episode/episode name

### Requirement: Unmatched items section
The unmatched items section SHALL group all items with outcome=Unmatched (2), collapsed by default when more than 5 items.

#### Scenario: Collapse long unmatched lists
- **WHEN** there are more than 5 unmatched items
- **THEN** the unmatched section SHALL show the first 5 with a "Show N more..." button

### Requirement: Ruleset link
The match detail view SHALL provide a link to the ruleset that produced the evaluation.

#### Scenario: Navigate to ruleset
- **WHEN** the user clicks the ruleset link in the match detail footer
- **THEN** the app SHALL navigate to `/rulesets/series/:tvdbId` or the appropriate movie path
