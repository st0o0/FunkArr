## ADDED Requirements

### Requirement: Channels editor
The ruleset editor SHALL provide an input for the `channels` property on `RuleSetFile`. Channels SHALL be displayed as a comma-separated text input. The ruleset detail view SHALL display channels as badges.

#### Scenario: Edit channels
- **WHEN** the user enters "ARD, ZDF" in the channels input
- **THEN** the saved `RuleSetFile` SHALL have `channels: ["ARD", "ZDF"]`

#### Scenario: Display channels in detail
- **WHEN** a ruleset has `channels: ["ARD", "Das Erste"]`
- **THEN** the detail view SHALL display channel badges below the topic name

#### Scenario: Empty channels
- **WHEN** the user leaves the channels input empty
- **THEN** the saved `RuleSetFile` SHALL have `channels: null`

### Requirement: Auto-generation from detail view
The ruleset detail view SHALL provide a "Generate Rules" button that triggers auto-generation via the GenerateController.

#### Scenario: Generate preview
- **WHEN** the user clicks "Generate Rules" on a ruleset with a tvdbId
- **THEN** the UI SHALL call `POST /api/v1/generate/preview` and display the generated ruleset preview

#### Scenario: Apply generated rules
- **WHEN** the user reviews the preview and clicks "Apply"
- **THEN** the UI SHALL call `POST /api/v1/generate/apply` and reload the ruleset detail

### Requirement: Shared TypeScript types
All Vue components SHALL import TypeScript interfaces from a shared `types.ts` file instead of defining them inline.

#### Scenario: Types imported
- **WHEN** any Vue component references `Rule`, `Filter`, `FilterGroup`, `TitleRule`, or `RuleSetFile`
- **THEN** it SHALL import the type from `@/types`

## MODIFIED Requirements

### Requirement: Filter group editor
The editor SHALL provide a visual builder for filter groups supporting All, Any, and Not logical operators, with each filter having field, operator, and value inputs. The available fields SHALL be: `duration`, `title`, `description`, `topic`, `channel`, `timestamp`. The `topicTitle` composite field SHALL NOT be offered.

#### Scenario: Add filter
- **WHEN** the user clicks "Add Filter" in a rule
- **THEN** a new filter row SHALL appear with dropdowns for field (duration, title, description, topic, channel, timestamp), operator (GreaterThan, LessThan, ExactMatch, Contains, Regex, Eq, NotContains), and a text input for value

#### Scenario: topicTitle not available
- **WHEN** the user opens the field dropdown
- **THEN** `topicTitle` SHALL NOT appear as an option

### Requirement: Title rule editor
The editor SHALL provide inputs for title rules of type regex (field, pattern, capture group) and static (literal value). The available fields for regex title rules SHALL be: `title`, `topic`, `description`. The `topicTitle` composite field SHALL NOT be offered.

#### Scenario: Add regex title rule
- **WHEN** the user adds a regex title rule
- **THEN** the field dropdown SHALL offer `title`, `topic`, `description` (no `topicTitle`)

### Requirement: Live test against Mediathek
The editor SHALL provide a "Test" button that sends the current rules to the backend at `POST /api/v1/rulesets/{tvdbId}/test` and displays the match results.

#### Scenario: Test succeeds
- **WHEN** the user clicks "Test" with valid rules for a show with tvdbId 83214
- **THEN** the UI SHALL POST to `/api/v1/rulesets/83214/test` with `{ topic, tvdbId, rules }` and display matched, filtered, and unmatched items

### Requirement: Ruleset detail routing
The ruleset detail and editor views SHALL route by tvdbId (integer), not by topic string. The URL pattern SHALL be `/rulesets/{tvdbId}` and `/rulesets/{tvdbId}/edit`.

#### Scenario: Navigate from list
- **WHEN** the user clicks a ruleset row with tvdbId 83214
- **THEN** the browser SHALL navigate to `/rulesets/83214`

#### Scenario: API call uses tvdbId
- **WHEN** the detail view loads for route `/rulesets/83214`
- **THEN** the API call SHALL be `GET /api/v1/rulesets/83214`
