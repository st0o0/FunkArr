## MODIFIED Requirements

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
