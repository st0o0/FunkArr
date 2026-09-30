## MODIFIED Requirements

### Requirement: SPA shell with tab navigation
The system SHALL serve a Vue 3 single-page application from `wwwroot/` that provides a persistent tab bar for navigating between Queue, History, Rulesets, and Matches views. A gear icon with a health-status dot SHALL be displayed on the right side of the header bar, linking to the Settings view.

#### Scenario: Tab navigation
- **WHEN** a user clicks the "Rulesets" tab
- **THEN** the browser navigates to `/#/rulesets` and renders the rulesets view without a full page reload

#### Scenario: Direct URL access
- **WHEN** a user opens `http://funkarr:5000/#/rulesets/tatort` directly
- **THEN** the app loads and renders the Tatort ruleset detail view

#### Scenario: Settings access via gear icon
- **WHEN** a user clicks the gear icon in the header
- **THEN** the browser navigates to `/#/settings` and renders the settings view

#### Scenario: Settings not in tab list
- **WHEN** the header renders
- **THEN** "Settings" SHALL NOT appear as a tab in the navigation bar
