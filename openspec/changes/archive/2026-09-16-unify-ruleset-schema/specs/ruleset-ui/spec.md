## MODIFIED Requirements

### Requirement: RuleSet detail page
The Vue frontend SHALL render a ruleset detail page at route `/rulesets/:id`. On mount, the page SHALL fetch `GET /api/rulesets/:id` and display three sections: Identity, Source, and Matching Rules.

The Identity section SHALL show: topic, aliases, and media IDs (TVDB, IMDB, TMDB).

The Source section SHALL show: community file path and last modified timestamp, local file path and last modified timestamp, and the effective merge mode (community only, local only, merged, standalone).

The Matching Rules section SHALL show: default confidence, and for each rule: ID, priority, confidence override, identification strategy (formatted via shared `strategyLabel()` utility with i18n), strategy-specific parameters (season/episode regex, capture group, title rules), and structured filter conditions rendered as a component tree.

The strategy badge on each rule card SHALL display the localized strategy label from the shared utility, not a raw string. Filter conditions SHALL be rendered as a structured component showing field/op/value per condition grouped by all/any/not, not as a pre-formatted summary string. Title rules SHALL be rendered showing their type and parameters, not as pre-formatted strings.

The page SHALL include a link to the scoring history at `/rulesets/:id/history`.

#### Scenario: Detail with community + local overlay
- **WHEN** the user views a ruleset that has both community and local files
- **THEN** the source section shows both file paths with timestamps and merge mode "merged"

#### Scenario: Detail with community only
- **WHEN** the user views a ruleset with only a community file
- **THEN** the source section shows only the community path and merge mode "community only"

#### Scenario: Rule display with strategy label
- **WHEN** a rule uses strategy `"seasonAndEpisodeNumber"` and locale is `de`
- **THEN** the rule card shows the localized strategy label (e.g. "Staffel & Episodennummer") via the shared `strategyLabel()` utility

#### Scenario: Rule display with structured filters
- **WHEN** a rule has filter conditions `{ all: [{ field: "title", op: "contains", value: "Tatort" }] }`
- **THEN** the rule card renders the filter tree as structured components showing field, operator, and value — not a summary string

#### Scenario: Rule display with title rules
- **WHEN** a rule uses `itemTitleExact` with title rules `[{ type: "regex", field: "title", pattern: "(.+)", captureGroup: 1 }]`
- **THEN** the rule card renders each title rule showing type, field, pattern, and capture group

#### Scenario: Rule display without matchMode
- **WHEN** any rule is displayed on the detail page
- **THEN** no "Match Mode" row SHALL be rendered (the field no longer exists)

#### Scenario: Not found
- **WHEN** the user navigates to `/rulesets/nonexistent` and the API returns 404
- **THEN** the page displays a "not found" message

## ADDED Requirements

### Requirement: Shared strategy label utility
The frontend SHALL provide a shared utility function `strategyLabel(strategy: string): string` that maps strategy wire enum names to localized display labels via `$t()`. The utility SHALL be importable by all views (Detail-View, Builder, Debugger, ScoringDetail). The utility SHALL handle all known strategy values: `seasonAndEpisodeNumber`, `byAbsoluteEpisodeNumber`, `itemTitleExact`, `itemTitleIncludes`, `itemTitleEqualsAirdate`. Unknown values SHALL fall back to the raw string.

#### Scenario: Strategy label in German locale
- **WHEN** `strategyLabel("itemTitleExact")` is called with locale `de`
- **THEN** the function returns the German translation for title exact match

#### Scenario: Strategy label for unknown value
- **WHEN** `strategyLabel("unknownStrategy")` is called
- **THEN** the function returns `"unknownStrategy"` as fallback

#### Scenario: Shared across views
- **WHEN** the Detail-View, Builder, Debugger, and ScoringDetail render strategy labels
- **THEN** all views SHALL use the same shared utility function

### Requirement: Filter condition display component
The frontend SHALL provide a component for rendering structured filter conditions (field/op/value grouped by all/any/not) in a read-only display format. This component SHALL be usable by the Detail-View for showing rule filters. The component SHALL render filter groups with section labels (all/any/not) and individual conditions showing field, operator, and value.

#### Scenario: Render all-group filters
- **WHEN** a filter has `all: [{ field: "title", op: "contains", value: "Tatort" }, { field: "duration", op: "greaterThan", value: "600" }]`
- **THEN** the component renders both conditions under an "all" group label

#### Scenario: Render mixed groups
- **WHEN** a filter has conditions in both `all` and `not` groups
- **THEN** the component renders both groups with their respective labels

#### Scenario: Empty filters
- **WHEN** a rule has no filters (null)
- **THEN** no filter component is rendered
