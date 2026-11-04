# playwright-layout-tests Specification

## Purpose

Tests for the AppLayout shell: sidebar navigation, collapse/expand, locale switching, and active route highlighting.

## Requirements

### Requirement: Sidebar navigation

The sidebar SHALL display navigation links and correctly highlight the active route.

#### Scenario: All nav items visible
- **WHEN** the sidebar is expanded
- **THEN** 5 navigation items SHALL be visible: Overview, Activity, RuleSets, Feed, Settings

#### Scenario: Active route highlighted
- **WHEN** the user is on `/activity`
- **THEN** the Activity nav item SHALL have the active styling (accent border)

#### Scenario: Navigation between routes
- **WHEN** the user clicks the "RuleSets" nav item
- **THEN** the URL SHALL change to `/rulesets` and the RuleSets nav item SHALL become active

### Requirement: Sidebar collapse

The sidebar SHALL support collapsing to icon-only mode with state persisted in localStorage.

#### Scenario: Collapse toggle
- **WHEN** the user clicks the collapse button
- **THEN** the sidebar SHALL collapse to show only icons, hiding labels and the language selector

#### Scenario: Expand toggle
- **WHEN** the sidebar is collapsed and the user clicks the expand button
- **THEN** the sidebar SHALL expand to show labels and the language selector

#### Scenario: Collapse state persists across navigation
- **WHEN** the sidebar is collapsed and the user navigates to another route
- **THEN** the sidebar SHALL remain collapsed

#### Scenario: Collapse state restored on reload
- **WHEN** localStorage contains `funkarr-sidebar=collapsed` and the page loads
- **THEN** the sidebar SHALL start in collapsed state

### Requirement: Locale switching

The language selector SHALL change the UI locale and persist the selection.

#### Scenario: Switch to German
- **WHEN** the user selects "Deutsch" from the language dropdown
- **THEN** visible UI text SHALL switch to German (e.g., "Übersicht" instead of "Overview")

#### Scenario: Locale persists on reload
- **WHEN** the locale is set to "de" and the page reloads
- **THEN** the UI SHALL render in German

### Requirement: Version and footer

The sidebar footer SHALL display the app version when expanded.

#### Scenario: Version visible when expanded
- **WHEN** the sidebar is expanded
- **THEN** the version string SHALL be visible in the footer

#### Scenario: Version hidden when collapsed
- **WHEN** the sidebar is collapsed
- **THEN** the version string SHALL NOT be visible
