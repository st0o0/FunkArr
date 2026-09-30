## MODIFIED Requirements

### Requirement: Version endpoint
The system SHALL respond to `GET /download/api?mode=version` with a JSON object containing a SABnzbd version string. The version string SHALL be defined as a named constant, not an inline literal.

#### Scenario: Version response
- **WHEN** `?mode=version` is requested
- **THEN** the response SHALL be JSON `{"version":"4.3.3"}`

#### Scenario: Version string is a constant
- **WHEN** the SABnzbd version endpoint code is inspected
- **THEN** the version string SHALL be referenced from a named constant (e.g., `SabnzbdConstants.Version`)
