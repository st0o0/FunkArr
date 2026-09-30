## MODIFIED Requirements

### Requirement: SPA shell with tab navigation
The system SHALL serve a Vue 3 single-page application from `wwwroot/` that provides a persistent tab bar for navigating between Queue, History, and Rulesets views. A gear icon with a health-status dot SHALL be displayed on the right side of the header bar, linking to the Settings view.

#### Scenario: Tab navigation
- **WHEN** a user clicks the "Rulesets" tab
- **THEN** the browser navigates to `/#/rulesets` and renders the rulesets view without a full page reload

#### Scenario: Direct URL access
- **WHEN** a user opens `http://funkarr:5000/#/rulesets/series/83214` directly
- **THEN** the app loads and renders the Tatort ruleset detail view

#### Scenario: Settings access via gear icon
- **WHEN** a user clicks the gear icon in the header
- **THEN** the browser navigates to `/#/settings` and renders the settings view

#### Scenario: Settings not in tab list
- **WHEN** the header renders
- **THEN** "Settings" SHALL NOT appear as a tab in the navigation bar

#### Scenario: Matches tab removed
- **WHEN** the header renders
- **THEN** "Matches" SHALL NOT appear as a tab in the navigation bar

### Requirement: Vue Router with hash mode
The app SHALL use Vue Router in hash mode (`createWebHashHistory`) for client-side routing.

#### Scenario: Route definition
- **WHEN** the app initializes
- **THEN** routes SHALL be registered for `/`, `/history`, `/rulesets`, `/rulesets/new`, `/rulesets/series/:tvdbId`, `/rulesets/series/:tvdbId/edit`, `/rulesets/movies/:tmdbId`, `/rulesets/movies/:tmdbId/edit`, and `/settings`

#### Scenario: Legacy matches redirect
- **WHEN** a user navigates to `/#/matches`
- **THEN** the router SHALL redirect to `/#/rulesets`

#### Scenario: Legacy match detail redirect
- **WHEN** a user navigates to `/#/matches/:id`
- **THEN** the router SHALL redirect to `/#/rulesets` (graceful fallback since the evaluation ID cannot be directly mapped to a ruleset without a lookup)
