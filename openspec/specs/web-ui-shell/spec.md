## Purpose

Vue 3 SPA shell: static file serving from the .NET host, hash-mode routing, tab navigation, and shared API client with apikey authentication.

## Requirements

### Requirement: SPA shell with tab navigation
The system SHALL serve a Vue 3 single-page application from `wwwroot/` that provides a persistent tab bar for navigating between Queue, History, Rulesets, Matches, and Settings views.

#### Scenario: Tab navigation
- **WHEN** a user clicks the "Rulesets" tab
- **THEN** the browser navigates to `/#/rulesets` and renders the rulesets view without a full page reload

#### Scenario: Direct URL access
- **WHEN** a user opens `http://funkarr:5000/#/rulesets/tatort` directly
- **THEN** the app loads and renders the Tatort ruleset detail view

### Requirement: Static file serving from .NET host
The ASP.NET application SHALL serve static files from `wwwroot/` using `UseStaticFiles`. All non-API, non-file requests SHALL fall through to `index.html` for SPA routing.

#### Scenario: Serve built assets
- **WHEN** a browser requests `/assets/index-abc123.js`
- **THEN** the server SHALL return the file from `wwwroot/assets/` with appropriate content type

#### Scenario: SPA fallback
- **WHEN** a browser requests `/rulesets` (not an API route, not a static file)
- **THEN** the server SHALL return `wwwroot/index.html`

### Requirement: API client with apikey authentication
The Vue app SHALL provide a shared API client that appends the `apikey` query parameter to all backend requests. The API key SHALL be stored in the browser's localStorage after initial setup.

#### Scenario: Authenticated API call
- **WHEN** the UI fetches `/api/v1/queue`
- **THEN** the request SHALL include `?apikey=<stored-key>` as a query parameter

#### Scenario: No API key stored
- **WHEN** no API key exists in localStorage
- **THEN** the app SHALL redirect to the setup wizard

### Requirement: Vue Router with hash mode
The app SHALL use Vue Router in hash mode (`createWebHashHistory`) for client-side routing.

#### Scenario: Route definition
- **WHEN** the app initializes
- **THEN** routes SHALL be registered for `/`, `/history`, `/rulesets`, `/rulesets/new`, `/rulesets/:topic`, `/rulesets/:topic/edit`, `/matches`, and `/settings`
