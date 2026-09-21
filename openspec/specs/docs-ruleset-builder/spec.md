## Purpose

Interactive Vue component in VitePress docs for visually constructing FunkArr ruleset JSON with validation, import, and export - without requiring a running FunkArr instance.

## Requirements

### Requirement: Builder page exists in both locales
The docs site SHALL have a builder page at `/rulesets/builder` (German) and `/en/rulesets/builder` (English). Both locale sidebar configurations SHALL include a "Builder" / "Regelwerk-Builder" entry under the Rulesets section.

#### Scenario: German builder page accessible
- **WHEN** a user navigates to `/rulesets/builder`
- **THEN** the page renders with a German heading and the interactive builder component

#### Scenario: English builder page accessible
- **WHEN** a user navigates to `/en/rulesets/builder`
- **THEN** the page renders with an English heading and the interactive builder component

#### Scenario: Sidebar entry visible in German
- **WHEN** the German docs sidebar renders
- **THEN** a "Regelwerk-Builder" entry appears under the "Regelwerke" section linking to `/rulesets/builder`

#### Scenario: Sidebar entry visible in English
- **WHEN** the English docs sidebar renders
- **THEN** a "Builder" entry appears under the "Rulesets" section linking to `/en/rulesets/builder`

### Requirement: Identity form
The builder SHALL provide form fields for all root-level and media-level ruleset properties: `topic` (required text), `aliases` (dynamic string list with add/remove), `media.name` (required text, auto-filled from topic), `media.type` (dropdown: show/movie), `media.tvdbId` (optional integer), `media.imdbId` (optional string), `media.tmdbId` (optional integer), and `confidence` (number 0.0–1.0).

#### Scenario: Topic field required
- **WHEN** the user leaves the topic field empty
- **THEN** a validation error indicates the topic is required

#### Scenario: Media name auto-fills from topic
- **WHEN** the user types "Tatort" in the topic field and media name is empty
- **THEN** the media name field auto-fills with "Tatort"

#### Scenario: Media name does not overwrite manual input
- **WHEN** the user has manually set the media name to "Tatort Wien"
- **AND** the user changes the topic to "Tatort aus Österreich"
- **THEN** the media name remains "Tatort Wien"

#### Scenario: Add and remove aliases
- **WHEN** the user clicks "Add Alias" and enters "Tatort aus Österreich"
- **THEN** the alias appears in the alias list
- **WHEN** the user clicks remove on that alias
- **THEN** it is removed from the list

#### Scenario: Confidence slider range
- **WHEN** the user adjusts the confidence value
- **THEN** only values between 0.0 and 1.0 (inclusive) are accepted

### Requirement: Rule management
The builder SHALL allow adding, removing, and reordering rules. Each rule SHALL have fields for `id` (required kebab-case string), `priority` (integer, default 0), `strategy` (required dropdown), and optional `confidence` override.

#### Scenario: Add a new rule
- **WHEN** the user clicks "Add Rule"
- **THEN** a new rule card appears with a generated ID and default priority 0

#### Scenario: Remove a rule
- **WHEN** the user clicks the remove button on a rule card
- **THEN** that rule is removed from the list and the JSON preview updates

#### Scenario: Duplicate rule ID validation
- **WHEN** two rules have the same `id` value
- **THEN** a validation error indicates duplicate rule IDs

#### Scenario: Rule ID format validation
- **WHEN** the user enters a rule ID that is not kebab-case or is fewer than 3 characters
- **THEN** a validation error indicates the ID format is invalid

### Requirement: Strategy-dependent fields
The builder SHALL show or hide rule fields based on the selected strategy. Regex-based strategies (`seasonAndEpisodeNumber`, `byAbsoluteEpisodeNumber`) SHALL show regex input fields. Title-based strategies (`itemTitleExact`, `itemTitleIncludes`, `itemTitleEqualsAirdate`) SHALL show the title rules builder.

#### Scenario: Season and episode strategy shows regex fields
- **WHEN** the user selects strategy "seasonAndEpisodeNumber"
- **THEN** the rule card shows `seasonRegex`, `episodeRegex`, and `captureGroup` fields
- **AND** the title rules builder is hidden

#### Scenario: Absolute episode strategy shows episode regex only
- **WHEN** the user selects strategy "byAbsoluteEpisodeNumber"
- **THEN** the rule card shows `episodeRegex` and `captureGroup` fields
- **AND** the `seasonRegex` field and title rules builder are hidden

#### Scenario: Title strategy shows title rules builder
- **WHEN** the user selects strategy "itemTitleIncludes"
- **THEN** the rule card shows the title rules builder
- **AND** regex input fields are hidden

### Requirement: Filter builder
Each rule SHALL have an optional filter section with three collapsible groups: ALL (AND logic), ANY (OR logic), and NOT (negation). Each group contains a list of conditions. Each condition has a field dropdown (title, topic, channel, description, duration, timestamp), an operator dropdown (eq, contains, notContains, greaterThan, lessThan, regex), and a value text input.

#### Scenario: Add a filter condition to ALL
- **WHEN** the user clicks "Add Condition" in the ALL section
- **THEN** a new condition row appears with empty field, operator, and value

#### Scenario: Remove a filter condition
- **WHEN** the user clicks the remove button on a condition row
- **THEN** that condition is removed

#### Scenario: ALL section expanded by default
- **WHEN** a rule card renders
- **THEN** the ALL filter section is expanded and ANY/NOT sections are collapsed

#### Scenario: Empty filter sections omitted from JSON
- **WHEN** all three filter sections are empty
- **THEN** the `filters` key is omitted from the rule in the JSON output

### Requirement: Title rules builder
For title-based strategies, the builder SHALL provide a title rules editor. Users can add ordered parts of type `static` (literal text value) or `regex` (field dropdown, pattern, optional capture group). Parts are displayed in order and can be removed.

#### Scenario: Add a static title part
- **WHEN** the user adds a title part with type "static" and value " - "
- **THEN** the part appears in the ordered list

#### Scenario: Add a regex title part
- **WHEN** the user adds a title part with type "regex", field "title", and pattern "(?<=Tatort:\s*)\S.*"
- **THEN** the part appears in the list with field and pattern displayed

#### Scenario: Invalid regex pattern shows error
- **WHEN** the user enters an invalid regex pattern like "(?<=unclosed"
- **THEN** a validation error appears on that title part indicating invalid regex syntax

### Requirement: Live JSON preview
The builder SHALL display a live JSON preview that updates reactively as the user modifies any field. The preview SHALL use syntax highlighting. Empty optional fields, null values, and empty arrays SHALL be omitted from the output.

#### Scenario: Preview updates on field change
- **WHEN** the user changes the topic from "Tatort" to "Polizeiruf 110"
- **THEN** the JSON preview immediately reflects `"topic": "Polizeiruf 110"`

#### Scenario: Empty fields omitted
- **WHEN** the user has not set any aliases
- **THEN** the `aliases` key does not appear in the JSON output

#### Scenario: Syntax highlighting applied
- **WHEN** the JSON preview renders
- **THEN** keys, strings, numbers, and punctuation are styled with distinct colors

### Requirement: Copy and download
The builder SHALL provide a "Copy" button that copies the JSON output to the clipboard and a "Download" button that saves it as a `.json` file. The download filename SHALL be derived from the topic field in kebab-case (e.g., topic "Tatort" → `tatort.json`).

#### Scenario: Copy to clipboard
- **WHEN** the user clicks "Copy"
- **THEN** the JSON string is copied to the clipboard and a brief confirmation appears

#### Scenario: Download as file
- **WHEN** the user clicks "Download" and the topic is "Abenteuer Wald"
- **THEN** a file named `abenteuer-wald.json` is downloaded containing the builder JSON

### Requirement: JSON import
The builder SHALL provide an "Import JSON" button that opens a modal with a textarea. Pasting valid ruleset JSON and confirming SHALL populate all builder fields from the imported data.

#### Scenario: Import valid JSON
- **WHEN** the user pastes valid ruleset JSON into the import modal and clicks "Import"
- **THEN** the builder fields populate with the imported values and the modal closes

#### Scenario: Import invalid JSON
- **WHEN** the user pastes malformed JSON and clicks "Import"
- **THEN** an error message appears in the modal and the builder state is unchanged

#### Scenario: Import overwrites current state
- **WHEN** the builder already has data and the user imports new JSON
- **THEN** all builder fields are replaced with the imported data

### Requirement: Client-side validation
The builder SHALL validate the ruleset on every change with debounced feedback. Validation errors (missing required fields, invalid regex, duplicate IDs) SHALL appear inline next to the relevant field. Warnings (no external ID set, zero rules) SHALL appear in the validation summary area.

#### Scenario: Missing strategy shows error
- **WHEN** a rule has no strategy selected
- **THEN** a validation error appears on that rule indicating a strategy is required

#### Scenario: Invalid regex shows error
- **WHEN** a regex field contains an invalid pattern
- **THEN** a validation error appears next to that field

#### Scenario: No external ID shows warning
- **WHEN** no tvdbId, imdbId, or tmdbId is set
- **THEN** a warning appears suggesting to add at least one external ID

### Requirement: Responsive layout
The builder SHALL display side-by-side on desktop (form left, JSON preview right) and stack vertically on mobile (form above, JSON preview below). The layout breakpoint SHALL be consistent with VitePress's responsive behavior.

#### Scenario: Desktop layout
- **WHEN** the viewport is wider than 960px
- **THEN** the form and JSON preview display side-by-side

#### Scenario: Mobile layout
- **WHEN** the viewport is narrower than 960px
- **THEN** the form displays above the JSON preview in a single column

### Requirement: Docs favicon
The docs site SHALL display the same favicon as the FunkArr UI. The file `docs/public/logo.svg` SHALL be a copy of `src/FunkArr.UI/public/favicon.svg`.

#### Scenario: Favicon visible
- **WHEN** the docs site loads in a browser
- **THEN** the browser tab shows the FunkArr amber audio bars favicon

#### Scenario: Logo visible in nav
- **WHEN** the docs site renders
- **THEN** the VitePress nav bar shows the FunkArr logo SVG
