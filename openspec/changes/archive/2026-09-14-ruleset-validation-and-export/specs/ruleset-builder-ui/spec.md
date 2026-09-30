## ADDED Requirements

### Requirement: Display validation errors in builder
The ruleset builder page SHALL display server-side validation errors returned from the API when a save fails with 422.

#### Scenario: Show errors after failed save
- **WHEN** the user clicks Save and the API returns 422 with 3 validation errors
- **THEN** the builder displays all 3 errors near the top of the form with the field path and fix instruction

#### Scenario: Clear errors on retry
- **WHEN** the user modifies the form and clicks Save again
- **THEN** previous validation errors are cleared before the new request is sent

#### Scenario: Scroll to errors
- **WHEN** validation errors are displayed
- **THEN** the page scrolls to make the error list visible

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
