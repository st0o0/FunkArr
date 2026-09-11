## Purpose

User-toggleable sidebar collapse between icon-only (52px) and full (192px) modes with localStorage persistence, title attributes, and smooth transitions.

## Requirements

### Requirement: Sidebar toggle between collapsed and expanded
The sidebar SHALL support two modes:
- **Collapsed**: 52px wide, showing only navigation icons
- **Expanded**: 192px wide, showing icons and text labels

A toggle button MUST be present in the sidebar to switch between modes.

#### Scenario: Toggle from expanded to collapsed
- **WHEN** the user clicks the sidebar toggle button while the sidebar is expanded
- **THEN** the sidebar width MUST animate from 192px to 52px
- **AND** navigation labels MUST be hidden
- **AND** the "FunkArr" text MUST be hidden (logo icon remains)

#### Scenario: Toggle from collapsed to expanded
- **WHEN** the user clicks the sidebar toggle button while the sidebar is collapsed
- **THEN** the sidebar width MUST animate from 52px to 192px
- **AND** navigation labels MUST become visible
- **AND** the "FunkArr" text MUST become visible

### Requirement: Smooth width transition
The sidebar width change SHALL use a CSS transition with duration 200ms and easing `ease`.

#### Scenario: Animation smoothness
- **WHEN** the sidebar transitions between collapsed and expanded
- **THEN** the width change MUST be animated (not instant)
- **AND** the content area MUST reflow smoothly alongside the sidebar

### Requirement: State persistence in localStorage
The sidebar collapsed/expanded state SHALL be persisted in `localStorage` under the key `funkarr-sidebar`.

#### Scenario: State saved on toggle
- **WHEN** the user toggles the sidebar
- **THEN** the new state (`collapsed` or `expanded`) MUST be written to `localStorage` key `funkarr-sidebar`

#### Scenario: State restored on page load
- **WHEN** the application loads
- **THEN** the sidebar MUST read from `localStorage` key `funkarr-sidebar`
- **AND** restore the previously saved state
- **AND** if no saved state exists, the sidebar MUST default to **expanded**

### Requirement: Icon-only mode with title attributes
When the sidebar is collapsed, each navigation item SHALL show only its icon. A `title` attribute MUST be set on each item showing the label text.

#### Scenario: Title attribute in collapsed mode
- **WHEN** the sidebar is collapsed
- **AND** the user hovers over a navigation icon
- **THEN** the browser's native tooltip MUST appear showing the navigation item's text label

#### Scenario: No title attribute in expanded mode
- **WHEN** the sidebar is expanded
- **THEN** navigation items SHALL NOT have a `title` attribute (the label is already visible)
