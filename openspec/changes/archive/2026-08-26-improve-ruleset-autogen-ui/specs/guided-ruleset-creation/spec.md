## ADDED Requirements

### Requirement: Guided ruleset creation wizard
The system SHALL replace the direct route to `RulesetEditor` at `/rulesets/new` with a `RulesetCreationWizard` component that guides users through: search → connect → preview → edit/save.

#### Scenario: Wizard is the default new-ruleset entry point
- **WHEN** the user navigates to `/rulesets/new` or clicks "New Ruleset" on the rulesets list
- **THEN** the system SHALL render the `RulesetCreationWizard` view instead of the bare `RulesetEditor`

### Requirement: Step 1 — MediathekViewWeb search
The wizard SHALL provide a text input for searching MediathekViewWeb. The search SHALL use the `POST /api/v1/generate/preview` endpoint with the `query` field.

#### Scenario: User searches for a topic
- **WHEN** the user enters "Tatort" and clicks "Search"
- **THEN** the system SHALL call `POST /api/v1/generate/preview` with `{ "type": "show", "query": "Tatort" }` and display the Mediathek item count, detected channels, and duration range from the response

#### Scenario: Movie search
- **WHEN** the user selects "Movie" as the media type and searches for "Das Boot"
- **THEN** the system SHALL call `POST /api/v1/generate/preview` with `{ "type": "movie", "query": "Das Boot" }`

#### Scenario: No results found
- **WHEN** the search returns no Mediathek items
- **THEN** the wizard SHALL display a message indicating no items were found and allow the user to try a different search term

### Requirement: Step 2 — TVDB/TMDB connection
The wizard SHALL display TVDB/TMDB search candidates from the preview response and allow the user to select one or manually enter an ID.

#### Scenario: Multiple candidates returned
- **WHEN** the preview response includes a `candidates` array with multiple entries
- **THEN** the wizard SHALL display each candidate with name, year, and overview, allowing the user to select one

#### Scenario: Manual ID entry
- **WHEN** the user prefers to enter a TVDB/TMDB ID manually
- **THEN** the wizard SHALL provide an ID input field and a "Use this ID" button that re-runs the preview with the explicit ID

#### Scenario: Single candidate auto-selected
- **WHEN** the preview response includes exactly one candidate
- **THEN** the wizard SHALL pre-select it and allow the user to confirm or enter a different ID

### Requirement: Step 3 — Preview and save
After connecting to TVDB/TMDB, the wizard SHALL display the generated rules and test traces using the `GeneratePreviewPanel` component.

#### Scenario: Preview with test results
- **WHEN** the user completes Step 2 and rules are generated
- **THEN** the wizard SHALL display the `GeneratePreviewPanel` with generated rules, test traces, and confidence score

#### Scenario: Save from wizard
- **WHEN** the user clicks "Save" in the preview step
- **THEN** the system SHALL call `POST /api/v1/generate/apply` with the generated ruleset and navigate to the new ruleset's detail page

#### Scenario: Edit before saving
- **WHEN** the user clicks "Edit Rules" in the preview step
- **THEN** the system SHALL navigate to the `RulesetEditor` pre-populated with the generated rules and the connected TVDB/TMDB ID

### Requirement: Manual creation fallback
The wizard SHALL provide a "Create Manually" link that navigates directly to the `RulesetEditor` with an empty form, preserving the existing manual workflow.

#### Scenario: Skip wizard
- **WHEN** the user clicks "Create Manually" at any step
- **THEN** the system SHALL navigate to the `RulesetEditor` with an empty form
