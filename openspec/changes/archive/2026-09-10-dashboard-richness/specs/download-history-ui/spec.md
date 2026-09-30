## MODIFIED Requirements

### Requirement: History page category filter
The History page SHALL support filtering by category via a dropdown populated from the dedicated categories endpoint.

#### Scenario: Category dropdown population
- **WHEN** the History page mounts
- **THEN** the system SHALL fetch `GET /api/downloads/history/categories` to populate the category filter dropdown

#### Scenario: Filter by category
- **WHEN** the user selects category "sonarr" from the filter
- **THEN** the system SHALL fetch `GET /api/downloads/history?category=sonarr&start=0&limit=25`
- **AND** display only matching items

#### Scenario: Clear filter
- **WHEN** the user clears the category filter
- **THEN** the system SHALL fetch all history items without a category filter

#### Scenario: Categories endpoint failure
- **WHEN** the categories endpoint returns an error
- **THEN** the category filter SHALL be hidden
