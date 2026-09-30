## ADDED Requirements

### Requirement: Documentation covers all shipped features

The VitePress docs site and README SHALL accurately document all features that have shipped in the application. When a feature is implemented but not documented, it SHALL be added to the appropriate docs page.

#### Scenario: User looks up observability setup
- **WHEN** a user navigates to the observability docs page
- **THEN** they find instructions for configuring OpenTelemetry tracing, metrics, and the Aspire Dashboard

#### Scenario: User looks up Settings page
- **WHEN** a user reads the web-ui docs page
- **THEN** they find a section describing the Settings page including the log viewer

#### Scenario: User looks up Download Detail
- **WHEN** a user reads the web-ui docs page
- **THEN** they find a section describing the Download Detail view accessible by clicking queue cards

### Requirement: README reflects current project state

The README SHALL contain correct configuration variable names, current test project names, and an up-to-date features list.

#### Scenario: Config prefix is correct
- **WHEN** a user reads the README config table
- **THEN** scoring history variables use the `FunkArr__ScoringHistory__` prefix

#### Scenario: Test commands are runnable
- **WHEN** a user copies test commands from the README
- **THEN** all referenced test project paths exist and the commands succeed

### Requirement: Landing page represents key features

The landing page SHALL display at least 6 feature tiles covering the project's major capabilities.

#### Scenario: New user sees feature overview
- **WHEN** a user visits the docs landing page
- **THEN** they see tiles for Newznab indexer, SABnzbd client, community rulesets, single container, proxy/geo-routing, and observability
