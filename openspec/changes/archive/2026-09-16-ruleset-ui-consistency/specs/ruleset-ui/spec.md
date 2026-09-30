## MODIFIED Requirements

### Requirement: RuleSet detail page
The Vue frontend SHALL render a ruleset detail page at route `/rulesets/:id`. On mount, the page SHALL fetch `GET /api/rulesets/:id` and display three sections: Identity, Source, and Matching Rules.

The Identity section SHALL show: topic, aliases, and media IDs (TVDB, IMDB, TMDB).

The Source section SHALL show: community file path and last modified timestamp, local file path and last modified timestamp, and the effective merge mode (community only, local only, merged, standalone).

The Matching Rules section SHALL show: default confidence, and for each rule a collapsible card. The collapsed state SHALL show the rule ID in monospace, the localized strategy badge, and priority. The expanded state SHALL show all rule details: strategy-specific parameters (season/episode regex, capture group), title rules rendered via `TitleRuleDisplay` component, and filter conditions rendered via `FilterConditionDisplay` component. The first rule SHALL be expanded by default; all others collapsed.

The page SHALL include a link to the scoring history at `/rulesets/:id/history`.

#### Scenario: Collapsible rule cards
- **WHEN** a ruleset with 4 rules is displayed
- **THEN** the first rule card is expanded showing full details, and the other 3 are collapsed showing only ID, strategy badge, and priority

#### Scenario: Expand collapsed rule
- **WHEN** the user clicks a collapsed rule card header
- **THEN** the card expands to show full details

#### Scenario: Collapse expanded rule
- **WHEN** the user clicks an expanded rule card header
- **THEN** the card collapses to show only ID, strategy badge, and priority

#### Scenario: Rule with filters uses FilterConditionDisplay
- **WHEN** a rule has filter conditions
- **THEN** the expanded card renders filters via the `FilterConditionDisplay` component with localized op symbols and group labels

#### Scenario: Rule with title rules uses TitleRuleDisplay
- **WHEN** a rule uses a title construction strategy with title rules
- **THEN** the expanded card renders title rules via the `TitleRuleDisplay` component with type badges

#### Scenario: Detail with community + local overlay
- **WHEN** the user views a ruleset that has both community and local files
- **THEN** the source section shows both file paths with timestamps and merge mode "merged"

#### Scenario: Detail with community only
- **WHEN** the user views a ruleset with only a community file
- **THEN** the source section shows only the community path and merge mode "community only"

#### Scenario: Not found
- **WHEN** the user navigates to `/rulesets/nonexistent` and the API returns 404
- **THEN** the page displays a "not found" message
