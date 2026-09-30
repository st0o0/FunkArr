## MODIFIED Requirements

### Requirement: Identity section
The builder form SHALL include an Identity section with fields for: `ruleSetId` (text input, kebab-case, required, only editable on create), `topic` (text input, required), `aliases` (dynamic list of text inputs with add/remove), `mediaType` (radio or select: "show" or "movie", required, defaults to "show"), `mediaName` (text input, required, auto-filled from topic but independently editable), `tvdbId` (number input, optional), `imdbId` (text input, optional), and `tmdbId` (number input, optional).

The `mediaName` field SHALL auto-sync with `topic` as long as the user has not manually edited it. When loading an existing ruleset where `media.name` differs from `topic`, the auto-sync SHALL be disabled (the field is considered manually edited). When the user types directly into the `mediaName` field, auto-sync SHALL be permanently disabled for that session.

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
