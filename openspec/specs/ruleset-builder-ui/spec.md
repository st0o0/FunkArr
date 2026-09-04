## Purpose

Vue.js visual editor for creating and editing local rulesets with strategy picker, filter builder, and title rules builder.

## Requirements

### Requirement: RuleSet builder page
The Vue frontend SHALL render a ruleset builder page at route `/rulesets/new` for creating new rulesets and `/rulesets/:id/edit` for editing existing ones. The page SHALL use an asymmetric split-pane layout: the builder form on the left (wider) and the live debugger panel on the right (narrower). The grid SHALL use `grid-cols-[1fr_380px]` to give the form more space than the debugger. On the edit route, the page SHALL fetch `GET /api/rulesets/:id` and populate the builder form with the existing ruleset data.

#### Scenario: Navigate to create new ruleset
- **WHEN** the user navigates to `/rulesets/new`
- **THEN** the builder form renders with empty fields

#### Scenario: Navigate to edit existing ruleset
- **WHEN** the user navigates to `/rulesets/tatort/edit`
- **THEN** the builder form is populated with the existing ruleset data from the API

#### Scenario: Edit community ruleset
- **WHEN** the user edits a community-only ruleset
- **THEN** saving creates a local overlay file (not modifying community)

#### Scenario: Asymmetric split-pane layout
- **WHEN** the builder page renders
- **THEN** the builder form occupies the left pane (fluid, `1fr`) and the debugger panel occupies the right pane (fixed `380px`)

### Requirement: Identity section
The builder form SHALL include an Identity section with fields for: `ruleSetId` (text input, kebab-case, required, only editable on create), `topic` (text input, required), `aliases` (dynamic list of text inputs with add/remove), `tvdbId` (number input, optional), `imdbId` (text input, optional), and `tmdbId` (number input, optional).

#### Scenario: Set identity fields
- **WHEN** the user fills in topic "Tatort" and adds alias "Tatort - Münster"
- **THEN** the identity section shows topic and one alias entry

#### Scenario: Add alias
- **WHEN** the user clicks the add alias button
- **THEN** a new empty text input appears in the aliases list

#### Scenario: Remove alias
- **WHEN** the user clicks the remove button on an alias entry
- **THEN** the alias is removed from the list

#### Scenario: RuleSetId read-only on edit
- **WHEN** the user is on the edit route `/rulesets/tatort/edit`
- **THEN** the ruleSetId field is displayed but not editable

#### Scenario: RuleSetId validation
- **WHEN** the user enters "My Show!" in the ruleSetId field
- **THEN** a validation message indicates the ID must be kebab-case

### Requirement: Default confidence field
The builder form SHALL include a default confidence input (number, 0.0 to 1.0, step 0.01) at the ruleset level. This value applies to rules that do not override confidence.

#### Scenario: Set default confidence
- **WHEN** the user sets default confidence to 0.85
- **THEN** the value is stored and included in the saved JSON

#### Scenario: Default confidence range
- **WHEN** the user enters a value outside 0.0-1.0
- **THEN** the input is constrained to the valid range

### Requirement: Rules section
The builder form SHALL include a Rules section with a list of rule editors. Each rule editor SHALL be a collapsible card showing the rule ID and strategy as summary when collapsed. The section SHALL support adding new rules (with a generated default ID), removing rules, and reordering rules by priority.

#### Scenario: Add new rule
- **WHEN** the user clicks "Add Rule"
- **THEN** a new rule card appears with a generated ID, priority set to the next available value, and the strategy picker open

#### Scenario: Remove rule
- **WHEN** the user clicks the remove button on a rule card
- **THEN** the rule is removed from the list

#### Scenario: Reorder rules
- **WHEN** the user changes a rule's priority value
- **THEN** the rules list re-sorts by priority

#### Scenario: Collapse and expand rule
- **WHEN** the user clicks a rule card header
- **THEN** the rule card toggles between collapsed (showing ID + strategy summary) and expanded (showing all fields)

### Requirement: Rule editor fields
Each rule editor SHALL include: `id` (text input, required), `priority` (number input, integer), `confidence` (number input, optional, 0.0-1.0), and a strategy picker.

#### Scenario: Edit rule ID
- **WHEN** the user changes a rule ID to "main-regex"
- **THEN** the rule ID is updated in the form state

#### Scenario: Set rule confidence override
- **WHEN** the user sets rule confidence to 0.95
- **THEN** the rule-level confidence overrides the default

#### Scenario: Clear rule confidence
- **WHEN** the user clears the rule confidence field
- **THEN** the rule uses the ruleset default confidence

### Requirement: Strategy picker
The rule editor SHALL include a strategy dropdown with options: `seasonAndEpisodeNumber`, `byAbsoluteEpisodeNumber`, `itemTitleExact`, `itemTitleIncludes`, `itemTitleEqualsAirdate`. Selecting a strategy SHALL show the corresponding parameter fields and hide others.

#### Scenario: Select seasonAndEpisodeNumber
- **WHEN** the user selects "seasonAndEpisodeNumber"
- **THEN** fields for `seasonRegex`, `episodeRegex`, and `captureGroup` (optional) are shown

#### Scenario: Select byAbsoluteEpisodeNumber
- **WHEN** the user selects "byAbsoluteEpisodeNumber"
- **THEN** fields for `episodeRegex` and `captureGroup` (optional) are shown, but no `seasonRegex`

#### Scenario: Select itemTitleExact
- **WHEN** the user selects "itemTitleExact"
- **THEN** the title rules builder is shown

#### Scenario: Select itemTitleIncludes
- **WHEN** the user selects "itemTitleIncludes"
- **THEN** the title rules builder is shown

#### Scenario: Select itemTitleEqualsAirdate
- **WHEN** the user selects "itemTitleEqualsAirdate"
- **THEN** no additional parameter fields are shown

#### Scenario: Switch strategy clears previous parameters
- **WHEN** the user switches from "seasonAndEpisodeNumber" to "itemTitleExact"
- **THEN** the regex fields are cleared and the title rules builder appears

### Requirement: Title rules builder
When a TitleConstruction strategy is selected (itemTitleExact or itemTitleIncludes), the rule editor SHALL show a title rules builder. The builder SHALL display an ordered list of title parts, each with a type picker (`static` or `regex`). Static parts SHALL have a `value` text input. Regex parts SHALL have `field` (dropdown: title, topic, channel, description), `pattern` (text input), and `captureGroup` (optional number input). The builder SHALL support adding and removing title parts.

#### Scenario: Add static title part
- **WHEN** the user adds a title part with type "static" and value "Tatort: "
- **THEN** the part appears in the ordered list

#### Scenario: Add regex title part
- **WHEN** the user adds a title part with type "regex", field "title", and pattern "Tatort:\\s*(.+)"
- **THEN** the part appears with the regex configuration

#### Scenario: Remove title part
- **WHEN** the user clicks remove on a title part
- **THEN** the part is removed from the list

### Requirement: Filter builder
Each rule editor SHALL include a filter builder with three sections: ALL (all conditions must match), ANY (at least one must match), and NOT (none may match). Each section SHALL contain an ordered list of filter conditions. Each condition SHALL have a field dropdown (title, topic, channel, description, duration, timestamp), an operator dropdown (eq, contains, notContains, greaterThan, lessThan, regex), and a value text input. The builder SHALL support adding and removing conditions within each section.

#### Scenario: Add filter condition to ALL section
- **WHEN** the user adds a condition to the ALL section with field "duration", op "greaterThan", value "60"
- **THEN** the condition appears in the ALL section

#### Scenario: Add filter condition to NOT section
- **WHEN** the user adds a condition to the NOT section with field "title", op "contains", value "Trailer"
- **THEN** the condition appears in the NOT section

#### Scenario: Add filter condition to ANY section
- **WHEN** the user adds a condition to the ANY section with field "channel", op "eq", value "ARD"
- **THEN** the condition appears in the ANY section

#### Scenario: Remove filter condition
- **WHEN** the user clicks remove on a filter condition
- **THEN** the condition is removed from its section

#### Scenario: Empty filter sections
- **WHEN** a filter section has no conditions
- **THEN** the section is omitted from the saved JSON

### Requirement: Save ruleset
The builder SHALL include a "Save" button that serializes the form state to the RawRuleSet JSON format and sends it to the appropriate API endpoint. For new rulesets: `POST /api/rulesets`. For existing rulesets: `PUT /api/rulesets/:id`. On success, a toast notification SHALL be shown and the page SHALL navigate to the detail view at `/rulesets/:id`. On error, a toast notification SHALL display the error message.

#### Scenario: Save new ruleset
- **WHEN** the user fills in all required fields and clicks Save on the create page
- **THEN** a POST request is sent and on success a success toast displays "RuleSet created" and the browser navigates to `/rulesets/my-show`

#### Scenario: Save existing ruleset
- **WHEN** the user edits a ruleset and clicks Save on the edit page
- **THEN** a PUT request is sent and on success a success toast displays "RuleSet saved" and the browser navigates to `/rulesets/tatort`

#### Scenario: Save validation error
- **WHEN** the user clicks Save with missing required fields (ruleSetId or topic)
- **THEN** validation errors are displayed and no API request is sent

#### Scenario: Save API error
- **WHEN** the API returns an error (e.g., 409 Conflict for duplicate ID)
- **THEN** an error toast SHALL display the error message

### Requirement: Builder API client functions
The frontend SHALL expose API client functions for the write endpoints: `createRuleSet(data)` calling `POST /api/rulesets`, `updateRuleSet(id, data)` calling `PUT /api/rulesets/:id`, and `deleteRuleSet(id)` calling `DELETE /api/rulesets/:id`. All functions SHALL throw on non-2xx responses.

#### Scenario: Create function sends POST
- **WHEN** `createRuleSet` is called with ruleset data
- **THEN** a POST request is sent to `/api/rulesets` with the JSON body

#### Scenario: Update function sends PUT
- **WHEN** `updateRuleSet("tatort", data)` is called
- **THEN** a PUT request is sent to `/api/rulesets/tatort` with the JSON body

#### Scenario: Delete function sends DELETE
- **WHEN** `deleteRuleSet("my-show")` is called
- **THEN** a DELETE request is sent to `/api/rulesets/my-show`

### Requirement: Builder presentation
The builder form SHALL use `surface-raised` cards for each section (Identity, Rules). Form inputs SHALL use `surface-elevated` backgrounds with `border-default` borders. Section headings SHALL use `text-xs font-semibold uppercase tracking-wider text-text-muted`. The strategy picker SHALL use a styled dropdown. Rule cards SHALL have a `brand-500/20` left border accent when expanded.

#### Scenario: Form section rendering
- **WHEN** the builder form renders
- **THEN** Identity and Rules sections are separate `surface-raised` cards

#### Scenario: Rule card expanded state
- **WHEN** a rule card is expanded
- **THEN** it has a `brand-500/20` left border accent
