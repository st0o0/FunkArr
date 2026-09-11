## Purpose

Application shell layout with collapsible sidebar navigation, logo mark, and flat navigation for the main content area.

## Requirements

### Requirement: Sidebar navigation structure

The application layout SHALL use a CSS Grid with a collapsible sidebar and fluid main content area. The sidebar SHALL toggle between a collapsed state (52px, icon-only) and an expanded state (192px, icons + labels). The grid template SHALL be `grid-cols-[52px_1fr]` when collapsed and `grid-cols-[192px_1fr]` when expanded. The sidebar SHALL contain the FunkArr logo mark, navigation links, and a Setup gear icon at the bottom.

#### Scenario: Collapsed sidebar rendering
- **WHEN** the sidebar is in collapsed state
- **THEN** the layout renders as a two-column grid with a 52px sidebar showing only icons and the content area filling the remaining width

#### Scenario: Expanded sidebar rendering
- **WHEN** the sidebar is in expanded state
- **THEN** the layout renders as a two-column grid with a 192px sidebar showing icons and labels and the content area filling the remaining width

#### Scenario: Sidebar sections
- **WHEN** the sidebar renders
- **THEN** it SHALL contain three sections top-to-bottom: logo mark with optional "FunkArr" text, navigation links (flat list, no group headers), and a bottom utility area with Setup gear icon and collapse toggle

#### Scenario: Sidebar toggle button
- **WHEN** the sidebar renders
- **THEN** a toggle button is visible at the bottom that switches between collapsed and expanded states

#### Scenario: Sidebar width transition
- **WHEN** the sidebar toggles between collapsed and expanded
- **THEN** the width animates smoothly via CSS transition (200ms ease)

#### Scenario: Sidebar state persistence
- **WHEN** the user toggles the sidebar state
- **THEN** the state is persisted to `localStorage` under key `funkarr-sidebar`
- **AND** on next page load, the sidebar restores the persisted state

### Requirement: Navigation items

The sidebar SHALL contain four navigation items in order: Overview (`/`), Activity (`/activity`), RuleSets (`/rulesets`). Setup SHALL be rendered separately at the sidebar bottom as a gear icon. There SHALL be no section group headers ("Media", "System"). Each item SHALL show an icon. Labels SHALL be visible only when the sidebar is expanded.

#### Scenario: Active route indication
- **WHEN** the current route matches a navigation item
- **THEN** that item SHALL display with `bg-surface-elevated` background and `text-text-primary` color

#### Scenario: Inactive route styling
- **WHEN** the current route does not match a navigation item
- **THEN** that item SHALL use `text-text-secondary` color with hover to `text-text-body`

#### Scenario: Hover state
- **WHEN** the user hovers over an inactive navigation item
- **THEN** the item background SHALL change to `surface-elevated/50`

#### Scenario: Collapsed icon-only mode
- **WHEN** the sidebar is collapsed
- **THEN** each navigation item shows only its icon, centered in the 52px width
- **AND** a title attribute with the label appears on hover

#### Scenario: Setup gear icon at bottom
- **WHEN** the sidebar renders
- **THEN** the Setup item SHALL appear in the bottom utility area separated by a `border-t` from the main nav
- **AND** it SHALL use a gear/cog icon

#### Scenario: No section headers
- **WHEN** the sidebar renders in expanded mode
- **THEN** there SHALL be no "Media" or "System" group headers between nav items

### Requirement: RuleSets route matching

The RuleSets navigation item SHALL be active for the `/rulesets` route and all nested routes (`/rulesets/:id`, `/rulesets/:id/edit`, `/rulesets/:id/history`, `/rulesets/:id/history/:requestId`).

#### Scenario: Nested route keeps parent active
- **WHEN** the user navigates to `/rulesets/tagesschau/history`
- **THEN** the RuleSets navigation item is active

### Requirement: Main content area

The main content area SHALL have a `surface-base` background with `px-6 py-5` padding. Content width constraints SHALL be set per-view, not globally.

#### Scenario: Content area rendering
- **WHEN** any page renders its content
- **THEN** the content is within the main area with `px-6 py-5` padding and no global max-width constraint
