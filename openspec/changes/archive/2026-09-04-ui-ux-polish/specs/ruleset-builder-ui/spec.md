## MODIFIED Requirements

### Requirement: RuleSet builder page

The Vue frontend SHALL render a ruleset builder page at route `/rulesets/new` for creating new rulesets and `/rulesets/:id/edit` for editing existing ones. The page SHALL use an asymmetric split-pane layout: the builder form on the left (wider) and the live debugger panel on the right (narrower). The grid SHALL use `grid-cols-[1fr_380px]` to give the form more space than the debugger. On the edit route, the page SHALL fetch `GET /api/rulesets/:id` and populate the builder form with the existing ruleset data.

#### Scenario: Navigate to create new ruleset
- **WHEN** the user navigates to `/rulesets/new`
- **THEN** the builder form renders with empty fields

#### Scenario: Navigate to edit existing ruleset
- **WHEN** the user navigates to `/rulesets/tatort/edit`
- **THEN** the builder form is populated with the existing ruleset data from the API

#### Scenario: Asymmetric split-pane layout
- **WHEN** the builder page renders
- **THEN** the builder form occupies the left pane (fluid, `1fr`) and the debugger panel occupies the right pane (fixed `380px`)

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
