## Purpose

Comprehensive ruleset creation documentation covering field reference, strategy guide, filter cookbook, merge behavior, and debugger walkthrough.

## Requirements

### Requirement: Ruleset creation tutorial
The docs SHALL include a step-by-step tutorial page (`rulesets/custom.md`) that walks users through creating their first ruleset. The tutorial SHALL cover: opening the builder UI, filling in identity fields, adding a rule, choosing a strategy, testing with the debugger, and saving.

#### Scenario: Tutorial covers full workflow
- **WHEN** a user reads `rulesets/custom.md`
- **THEN** they can follow step-by-step instructions to create a working ruleset from scratch

#### Scenario: Tutorial includes screenshots or diagrams
- **WHEN** the tutorial describes UI interactions
- **THEN** it references specific UI elements by their labels and describes what to expect

### Requirement: Field reference page
The docs SHALL include a complete field reference page (`rulesets/field-reference.md`) documenting every field in the ruleset JSON format. Each field entry SHALL include: field name, type, required/optional, default value (if any), description, and an example value. Fields SHALL be organized by section: root fields, media fields, rule fields, filter fields, title rule fields.

#### Scenario: Root field documentation
- **WHEN** a user looks up the `topic` field
- **THEN** they find: name "topic", type "string", required, description "display name of the show or media used for Mediathek topic matching", example "Tatort"

#### Scenario: Rule field documentation
- **WHEN** a user looks up the `priority` field
- **THEN** they find: name "priority", type "integer", optional (default 0), description "sort order for rule evaluation — lower values are evaluated first"

#### Scenario: Local-only fields documented
- **WHEN** a user looks up `standalone` and `disable`
- **THEN** both are clearly marked as "local only" with explanations of when to use each

#### Scenario: Media reference fields
- **WHEN** a user looks up `media.tvdbId`
- **THEN** they find: name "tvdbId", type "integer", optional, description "TheTVDB series or movie ID for Sonarr/Radarr matching"

### Requirement: Strategy guide page
The docs SHALL include a strategy deep-dive page (`rulesets/strategies.md`) explaining all 5 identification strategies with real community ruleset examples.

#### Scenario: seasonAndEpisodeNumber documented
- **WHEN** a user reads about `seasonAndEpisodeNumber`
- **THEN** they understand it extracts season and episode numbers via regex patterns, with a real example showing `seasonRegex` and `episodeRegex` patterns

#### Scenario: byAbsoluteEpisodeNumber documented
- **WHEN** a user reads about `byAbsoluteEpisodeNumber`
- **THEN** they understand it extracts a single absolute episode number, with the Schloss Einstein example showing `episodeRegex: "\\((\\d{3,5})\\)"`

#### Scenario: itemTitleExact and itemTitleIncludes documented
- **WHEN** a user reads about title construction strategies
- **THEN** they understand how `titleRules` compose static and regex parts to build a title string, with the Tatort example showing static " - " combined with a regex extraction

#### Scenario: itemTitleEqualsAirdate documented
- **WHEN** a user reads about `itemTitleEqualsAirdate`
- **THEN** they understand it extracts an airdate from the title for shows identified by broadcast date, with the heute-show example

#### Scenario: Strategy selection guidance
- **WHEN** a user is unsure which strategy to use
- **THEN** the page includes a decision guide: "Use X when..." for each strategy

### Requirement: Filter cookbook page
The docs SHALL include a filter cookbook page (`rulesets/filters.md`) explaining the filter system in depth: all operators, all fields, nested group logic (ALL/AND, ANY/OR, NOT), and common patterns.

#### Scenario: Operator reference
- **WHEN** a user looks up filter operators
- **THEN** all 6 operators are documented with descriptions and examples: `eq` (exact match), `contains` (substring), `notContains` (exclude substring), `greaterThan` (numeric comparison), `lessThan` (numeric comparison), `regex` (pattern match)

#### Scenario: Field reference
- **WHEN** a user looks up filter fields
- **THEN** all 6 fields are documented: `title` (item title from Mediathek), `topic` (topic/show name), `channel` (broadcaster), `description` (item description), `duration` (length in minutes), `timestamp` (broadcast timestamp)

#### Scenario: Nested group logic explained
- **WHEN** a user reads about filter groups
- **THEN** they understand ALL (all conditions must match), ANY (at least one), NOT (none may match), and that groups can be nested for complex logic

#### Scenario: Common patterns
- **WHEN** a user wants to filter out short clips
- **THEN** the cookbook shows the pattern: `{ "field": "duration", "op": "greaterThan", "value": "25" }`

#### Scenario: Channel filter pattern
- **WHEN** a user wants to restrict to a specific broadcaster
- **THEN** the cookbook shows: `{ "field": "channel", "op": "eq", "value": "ARD" }`

### Requirement: Merge and override documentation
The ruleset guide pages SHALL document the community/local merge behavior: how local rulesets override community ones, the `standalone` flag, the `disable` list, field-by-field merge semantics for media and aliases, and rule-level override by ID.

#### Scenario: Merge behavior explained
- **WHEN** a user reads about merge behavior
- **THEN** they understand: local rules with the same ID replace community rules, new local rules are appended, aliases are unioned, media fields are merged with local winning per-field, confidence uses local if set

#### Scenario: Standalone mode explained
- **WHEN** a user reads about `standalone: true`
- **THEN** they understand it causes the local ruleset to be used as-is, ignoring the community base entirely

#### Scenario: Disable list explained
- **WHEN** a user reads about the `disable` array
- **THEN** they understand it contains rule IDs from the community base that should be skipped during merge

### Requirement: Debugger walkthrough
The ruleset tutorial SHALL include a section on using the built-in debugger panel to test rules against live Mediathek data. It SHALL explain: auto-fetched candidates, live preview vs full test, reading the rule pipeline trace, and interpreting filter/identification outcomes.

#### Scenario: Live preview explained
- **WHEN** a user reads about the debugger
- **THEN** they understand that candidates are auto-fetched from Mediathek and matched client-side in real time as rules are edited

#### Scenario: Full test explained
- **WHEN** a user reads about full test mode
- **THEN** they understand it sends rules and candidates to the server for accurate scoring with a detailed pipeline trace

#### Scenario: Reading trace results
- **WHEN** a user reads about trace interpretation
- **THEN** they understand the color coding: green (matched), red (filter failed), amber (identification failed), gray (skipped)
