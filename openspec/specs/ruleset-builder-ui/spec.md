## Purpose

Vue.js visual editor for creating and editing local rulesets with strategy picker, filter builder, and title rules builder.

## Requirements

### Requirement: RuleSet builder page
The Vue frontend SHALL render a ruleset builder page at route `/rulesets/new` for creating new rulesets and `/rulesets/:id/edit` for editing existing ones. The page SHALL use an asymmetric split-pane layout: the builder form on the left (wider) and the search + test panel on the right (narrower). The grid SHALL use `grid-cols-[1fr_380px]` within a `max-w-5xl mx-auto` container.

In edit mode, the builder SHALL fetch data from `GET /api/rulesets/:id` (the structured Detail endpoint) instead of a separate raw endpoint. The response fields SHALL be mapped directly to the form state. The `strategy` field SHALL already be in wire enum format (e.g. `"seasonAndEpisodeNumber"`), matching the form's select option values.

#### Scenario: Navigate to create new ruleset
- **WHEN** the user navigates to `/rulesets/new`
- **THEN** the builder form renders with empty fields and the search panel on the right

#### Scenario: Navigate to edit existing ruleset
- **WHEN** the user navigates to `/rulesets/tatort/edit`
- **THEN** the builder form is populated with the existing ruleset data from `GET /api/rulesets/tatort`

#### Scenario: Asymmetric split-pane layout
- **WHEN** the builder page renders
- **THEN** the builder form occupies the left pane (fluid, `1fr`) and the search/test panel occupies the right pane (fixed `380px`)
- **AND** the entire layout is constrained to `max-w-5xl mx-auto`

#### Scenario: Strategy label uses shared utility
- **WHEN** a rule's collapsed header shows the strategy label
- **THEN** the label SHALL be produced by the shared `strategyLabel()` utility, not a local function

### Requirement: Identity section
The builder form SHALL include an Identity section with fields for: `ruleSetId` (text input, kebab-case, required, only editable on create), `topic` (text input, required), `aliases` (dynamic list of text inputs with add/remove), `mediaType` (radio or select: "show" or "movie", required, defaults to "show"), `mediaName` (text input, required, auto-filled from topic but independently editable), `tvdbId` (number input, optional), `imdbId` (text input, optional), and `tmdbId` (number input, optional).

The `mediaName` field SHALL auto-sync with `topic` as long as the user has not manually edited it. When loading an existing ruleset where `media.name` differs from `topic`, the auto-sync SHALL be disabled (the field is considered manually edited). When the user types directly into the `mediaName` field, auto-sync SHALL be permanently disabled for that session.

All labels, headings, placeholders, and button text in this section SHALL use `$t()` translation calls instead of hardcoded strings.

#### Scenario: Set identity fields
- **WHEN** the user fills in topic "Tatort" and adds alias "Tatort - Münster"
- **THEN** the identity section shows topic, one alias entry, and mediaName auto-filled with "Tatort"

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

#### Scenario: Media type selector
- **WHEN** the builder renders
- **THEN** a media type selector with options "Show" and "Movie" SHALL be visible in the identity section
- **AND** the default selection SHALL be "show"

#### Scenario: Media name auto-sync from topic
- **WHEN** the user types "Tatort" in the topic field and has not edited mediaName
- **THEN** the mediaName field SHALL automatically show "Tatort"

#### Scenario: Media name manual override
- **WHEN** the user types "Leschs Kosmos" in the mediaName field
- **THEN** the mediaName field SHALL stop auto-syncing with topic for the rest of the session

#### Scenario: Media name loaded from existing ruleset
- **WHEN** editing a ruleset where `media.name` is "Checker Julian" but `topic` is "Checker Reportagen"
- **THEN** the mediaName field SHALL show "Checker Julian" and auto-sync SHALL be disabled

#### Scenario: Labels use translation keys
- **WHEN** the identity section renders with locale `de`
- **THEN** labels display German translations (e.g., "Thema" instead of "Topic", "Aliase" instead of "Aliases")

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
When a TitleConstruction strategy is selected (itemTitleExact or itemTitleIncludes), the rule editor SHALL show a title rules builder. Type picker options SHALL use localized labels from the shared `titlePartLabel()` utility. Field dropdown options for regex parts SHALL use localized labels from `fieldLabel()`. All option values SHALL remain as enum strings for serialization.

#### Scenario: Localized type picker
- **WHEN** the title part type picker renders with DE locale
- **THEN** options display localized labels (e.g. "Statisch", "Regex") while values remain "static", "regex"

#### Scenario: Localized field picker in regex part
- **WHEN** a regex title part's field dropdown renders with DE locale
- **THEN** options display localized labels (e.g. "Titel", "Thema") while values remain "title", "topic"

### Requirement: Filter builder
Each rule editor SHALL include a filter builder with three sections: ALL (all conditions must match), ANY (at least one must match), and NOT (none may match). Section headers SHALL display localized group labels from the shared `groupLabel()` utility instead of raw English strings. Each condition SHALL have a field dropdown showing localized field labels from `fieldLabel()` (with enum values preserved as option values), an operator dropdown showing localized operator labels from `opLabel()` (with enum values preserved as option values), and a value text input. The builder SHALL support adding and removing conditions within each section.

#### Scenario: Localized group headers
- **WHEN** the filter builder renders with DE locale
- **THEN** section headers display localized labels (e.g. "Alle erfüllt", "Mind. eins", "Keines") instead of "all", "any", "not"

#### Scenario: Localized op dropdown
- **WHEN** the operator dropdown renders with DE locale
- **THEN** options display localized labels (e.g. "größer als", "enthält", "gleich") while option values remain enum strings ("greaterThan", "contains", "eq")

#### Scenario: Localized field dropdown
- **WHEN** the field dropdown renders with DE locale
- **THEN** options display localized labels (e.g. "Titel", "Thema", "Dauer") while option values remain enum strings ("title", "topic", "duration")

#### Scenario: Add filter condition
- **WHEN** the user adds a condition to any section
- **THEN** the condition appears with localized dropdowns

#### Scenario: Remove filter condition
- **WHEN** the user clicks remove on a filter condition
- **THEN** the condition is removed from its section

### Requirement: Enrichment section in builder form
The RuleSet Builder form SHALL include an enrichment config section that allows editing all enrichment settings (enabled, methods, thresholds, tolerances, runtime mode).

#### Scenario: Enrichment section visible
- **WHEN** user opens the RuleSet Builder (create or edit)
- **THEN** an "Enrichment" section is visible between Default Confidence and Matching Rules

### Requirement: Enrichment config included in save
The serializeForm function SHALL include enrichment config in the request body sent to create/update endpoints.

#### Scenario: Save includes enrichment
- **WHEN** user modifies enrichment settings and saves the ruleset
- **THEN** the API request body includes the enrichment object with current values

### Requirement: Enrichment config loaded in edit mode
When loading a ruleset for editing, the builder SHALL populate the enrichment section from the detail API response.

#### Scenario: Load enrichment in edit mode
- **WHEN** user navigates to edit an existing ruleset
- **THEN** the enrichment section shows the ruleset's current enrichment config from the API

### Requirement: Save ruleset
The builder SHALL include a "Save" button that serializes the form state to the RawRuleSet JSON format and sends it to the appropriate API endpoint. For new rulesets: `POST /api/rulesets`. For existing rulesets: `PUT /api/rulesets/:id`. On success, a toast notification SHALL be shown and the page SHALL navigate to the detail view at `/rulesets/:id`. On error, a toast notification SHALL display the error message.

The serialized JSON SHALL include `media.name` and `media.type` as required by the schema. Optional ID fields (`tvdbId`, `imdbId`, `tmdbId`) SHALL be omitted when empty instead of sent as `null`. When editing a community ruleset, the serialized JSON SHALL include `standalone: true`.

#### Scenario: Save new ruleset
- **WHEN** the user fills in all required fields and clicks Save on the create page
- **THEN** a POST request is sent and on success a success toast displays "RuleSet created" and the browser navigates to `/rulesets/my-show`

#### Scenario: Save existing ruleset
- **WHEN** the user edits a ruleset and clicks Save on the edit page
- **THEN** a PUT request is sent and on success a success toast displays "RuleSet saved" and the browser navigates to `/rulesets/tatort`

#### Scenario: Serialized media includes name and type
- **WHEN** the user saves a ruleset with topic "Tatort", mediaType "show", and mediaName "Tatort"
- **THEN** the serialized JSON SHALL contain `"media": { "name": "Tatort", "type": "show", "tvdbId": 83214 }`

#### Scenario: Null optional IDs are omitted
- **WHEN** the user saves a ruleset with tvdbId 83214, no imdbId, and no tmdbId
- **THEN** the serialized media SHALL be `{ "name": "...", "type": "show", "tvdbId": 83214 }` without `imdbId` or `tmdbId` keys

#### Scenario: Community edit saves as standalone
- **WHEN** the user edits a community ruleset and clicks Save
- **THEN** the serialized JSON SHALL include `"standalone": true`

#### Scenario: Save validation error
- **WHEN** the user clicks Save with missing required fields (ruleSetId or topic)
- **THEN** validation errors are displayed and no API request is sent

#### Scenario: Save API error
- **WHEN** the API returns an error (e.g., 409 Conflict for duplicate ID)
- **THEN** an error toast SHALL display the error message

### Requirement: Builder API client functions
The frontend SHALL expose API client functions for the write endpoints: `createRuleSet(data)` calling `POST /api/rulesets`, `updateRuleSet(id, data)` calling `PUT /api/rulesets/:id`, and `deleteRuleSet(id)` calling `DELETE /api/rulesets/:id`. All functions SHALL throw on non-2xx responses. The `getRuleSetRaw(id)` function SHALL be removed - the Builder SHALL use `getRuleSetDetail(id)` instead.

#### Scenario: Create function sends POST
- **WHEN** `createRuleSet` is called with ruleset data
- **THEN** a POST request is sent to `/api/rulesets` with the JSON body

#### Scenario: Update function sends PUT
- **WHEN** `updateRuleSet("tatort", data)` is called
- **THEN** a PUT request is sent to `/api/rulesets/tatort` with the JSON body

#### Scenario: Delete function sends DELETE
- **WHEN** `deleteRuleSet("my-show")` is called
- **THEN** a DELETE request is sent to `/api/rulesets/my-show`

#### Scenario: Raw function removed
- **WHEN** the codebase is inspected
- **THEN** `getRuleSetRaw()` SHALL NOT exist in the API client

### Requirement: Builder presentation
The builder form SHALL use `surface-raised` cards for each section (Identity, Rules). Form inputs SHALL use `surface-elevated` backgrounds with `border-default` borders. Section headings SHALL use `text-sm font-semibold text-text-secondary` in normal case (not uppercase tracking-widest). The Save button SHALL use Primary button tier styling (amber accent background, black text). The Cancel button SHALL use Secondary button tier styling. All heading text and button labels SHALL use `$t()` translation calls.

#### Scenario: Form section rendering
- **WHEN** the builder form renders
- **THEN** Identity and Rules sections are separate `surface-raised` cards

#### Scenario: Section heading styling
- **WHEN** a section heading renders (e.g., "Identity", "Matching Rules")
- **THEN** it SHALL use `text-sm font-semibold text-text-secondary` in normal case
- **AND** SHALL NOT use `uppercase tracking-widest` or `tracking-wider`

#### Scenario: Save button primary tier
- **WHEN** the Save button renders
- **THEN** it SHALL use Primary button tier: `bg-accent text-black font-medium rounded-md`

#### Scenario: Cancel button secondary tier
- **WHEN** the Cancel button renders
- **THEN** it SHALL use Secondary button tier: `bg-surface-elevated border border-border-default text-text-body rounded-md`

#### Scenario: Translated headings in German
- **WHEN** the locale is `de` and the builder renders
- **THEN** section headings display "Identität", "Regeln" instead of "Identity", "Rules"

### Requirement: Display validation errors in builder
The ruleset builder page SHALL display server-side validation errors returned from the API when a save fails with 422. Error messages SHALL use translation keys with interpolation for dynamic parts.

#### Scenario: Show errors after failed save
- **WHEN** the user clicks Save and the API returns 422 with 3 validation errors
- **THEN** the builder displays all 3 errors near the top of the form with the field path and fix instruction

#### Scenario: Clear errors on retry
- **WHEN** the user modifies the form and clicks Save again
- **THEN** previous validation errors are cleared before the new request is sent

#### Scenario: Scroll to errors
- **WHEN** validation errors are displayed
- **THEN** the page scrolls to make the error list visible

#### Scenario: Validation error count translated
- **WHEN** 3 validation errors are shown with locale `de`
- **THEN** the header reads "3 Validierungsfehler:" instead of "3 validation errors:"

### Requirement: Export button on detail page
The ruleset detail page SHALL show an "Export for Community" button for rulesets that have a local component.

#### Scenario: Button visible for local ruleset
- **WHEN** the detail page shows a ruleset with `sourceType` "local" or "merged"
- **THEN** an "Export for Community" button is visible in the action bar alongside Edit and Delete

#### Scenario: Button hidden for community-only ruleset
- **WHEN** the detail page shows a ruleset with `sourceType` "community"
- **THEN** no export button is visible

#### Scenario: Click export
- **WHEN** the user clicks "Export for Community"
- **THEN** the browser downloads the exported JSON file via `GET /api/rulesets/{id}/export`

#### Scenario: Export fails with validation errors
- **WHEN** the export endpoint returns 422
- **THEN** the detail page displays the validation errors in a toast or error panel
