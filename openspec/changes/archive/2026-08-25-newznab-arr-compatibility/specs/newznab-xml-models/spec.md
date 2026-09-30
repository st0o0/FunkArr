## MODIFIED Requirements

### Requirement: Caps response model
The system SHALL provide a caps response either as a serializable model or as a static method, producing Newznab capabilities XML with server info, limits, search capabilities (search, tv-search, movie-search), and categories.

#### Scenario: Caps XML structure
- **WHEN** the caps endpoint is called
- **THEN** the response SHALL contain `<caps>` with `<server>`, `<limits>`, `<searching>`, and `<categories>` elements matching Newznab spec

#### Scenario: Limits element in caps
- **WHEN** the caps endpoint is called
- **THEN** the response SHALL contain `<limits max="100" default="100" />` after the `<server>` element
