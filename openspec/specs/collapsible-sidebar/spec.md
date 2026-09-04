## Purpose

User-toggleable sidebar collapse between icon-only (56px) and full (200px) modes with localStorage persistence, tooltips, and smooth transitions.

## Requirements

### Requirement: Sidebar toggle between collapsed and expanded
The sidebar SHALL support two modes:
- **Collapsed**: 56px wide, showing only navigation icons
- **Expanded**: 200px wide, showing icons and text labels

A toggle button MUST be present in the sidebar to switch between modes.

#### Scenario: Toggle from expanded to collapsed
- **WHEN** the user clicks the sidebar toggle button while the sidebar is expanded
- **THEN** the sidebar width MUST animate from 200px to 56px
- **AND** navigation labels MUST be hidden
- **AND** the section group headers ("Media", "System") MUST be hidden
- **AND** the version label MUST be hidden

#### Scenario: Toggle from collapsed to expanded
- **WHEN** the user clicks the sidebar toggle button while the sidebar is collapsed
- **THEN** the sidebar width MUST animate from 56px to 200px
- **AND** navigation labels MUST become visible
- **AND** the section group headers MUST become visible
- **AND** the version label MUST become visible

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
- **AND** if no saved state exists, the sidebar MUST default to expanded

### Requirement: Icon-only mode with tooltips
When the sidebar is collapsed, each navigation item SHALL show only its icon. A tooltip MUST appear on hover showing the item's label.

#### Scenario: Tooltip on hover in collapsed mode
- **WHEN** the sidebar is collapsed
- **AND** the user hovers over a navigation icon
- **THEN** a tooltip MUST appear showing the navigation item's text label

#### Scenario: No tooltip in expanded mode
- **WHEN** the sidebar is expanded
- **AND** the user hovers over a navigation item
- **THEN** no tooltip SHALL appear (the label is already visible)

### Requirement: Active nav item amber accent
The active navigation item SHALL use an amber left-border accent instead of the previous background-tint style.

#### Scenario: Active item visual indicator
- **WHEN** a navigation item corresponds to the current route
- **THEN** it MUST display a left-border accent using `brand-400` (amber)
- **AND** the icon and label text MUST use `brand-400`

#### Scenario: Inactive item appearance
- **WHEN** a navigation item does not correspond to the current route
- **THEN** it MUST NOT have a left-border accent
- **AND** text MUST use `text-secondary`

### Requirement: Logo adapts to collapsed state
The FunkArr logo area SHALL adapt when the sidebar is collapsed.

#### Scenario: Collapsed logo
- **WHEN** the sidebar is collapsed
- **THEN** only the logo icon MUST be visible (no "FunkArr" text)

#### Scenario: Expanded logo
- **WHEN** the sidebar is expanded
- **THEN** both the logo icon and "FunkArr" text MUST be visible
