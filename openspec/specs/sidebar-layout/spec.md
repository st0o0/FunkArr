## Purpose

Application shell layout with collapsible sidebar navigation, brand wordmark, responsive collapse, and breadcrumb bar for the main content area.

## Requirements

### Requirement: Sidebar navigation structure

The application layout SHALL use a CSS Grid with a collapsible sidebar and fluid main content area. The sidebar SHALL toggle between a collapsed state (56px, icon-only) and an expanded state (200px, icons + labels). The grid template SHALL be `grid-cols-[56px_1fr]` when collapsed and `grid-cols-[200px_1fr]` when expanded. The sidebar SHALL contain the FunkArr wordmark (expanded only), navigation links, and a version footer.

#### Scenario: Collapsed sidebar rendering
- **WHEN** the sidebar is in collapsed state
- **THEN** the layout renders as a two-column grid with a 56px sidebar showing only icons and the content area filling the remaining width

#### Scenario: Expanded sidebar rendering
- **WHEN** the sidebar is in expanded state
- **THEN** the layout renders as a two-column grid with a 200px sidebar showing icons and labels and the content area filling the remaining width

#### Scenario: Sidebar sections
- **WHEN** the sidebar renders
- **THEN** it contains three sections top-to-bottom: brand wordmark (hidden when collapsed), navigation links, version number (hidden when collapsed)

#### Scenario: Sidebar toggle button
- **WHEN** the sidebar renders
- **THEN** a toggle button is visible that switches between collapsed and expanded states

#### Scenario: Sidebar width transition
- **WHEN** the sidebar toggles between collapsed and expanded
- **THEN** the width animates smoothly via CSS transition (200ms ease)

#### Scenario: Sidebar state persistence
- **WHEN** the user toggles the sidebar state
- **THEN** the state is persisted to `localStorage` under key `funkarr-sidebar`
- **AND** on next page load, the sidebar restores the persisted state

### Requirement: Sidebar brand wordmark

The sidebar SHALL display "FunkArr" as a text wordmark using the `brand-500` color, bold weight, and tight tracking.

#### Scenario: Wordmark rendering
- **WHEN** the sidebar renders
- **THEN** the wordmark "FunkArr" appears at the top in `brand-500` color with `font-bold` and `tracking-tight`

### Requirement: Navigation items

The sidebar SHALL contain five navigation items in order: Dashboard (`/`), Queue (`/queue`), History (`/history`), RuleSets (`/rulesets`), and Setup (`/setup`). Each item SHALL show an icon. Labels SHALL be visible only when the sidebar is expanded.

#### Scenario: Active route indication
- **WHEN** the current route matches a navigation item
- **THEN** that item displays a 2px left border in `brand-500` and a background tint of `brand-900/20`, with `text-primary` color

#### Scenario: Inactive route styling
- **WHEN** the current route does not match a navigation item
- **THEN** that item uses `text-secondary` color with no left border

#### Scenario: Hover state
- **WHEN** the user hovers over an inactive navigation item
- **THEN** the item background changes to `surface-elevated` and text changes to `text-body`

#### Scenario: Collapsed icon-only mode
- **WHEN** the sidebar is collapsed
- **THEN** each navigation item shows only its icon, centered in the 56px width
- **AND** a tooltip with the label appears on hover

#### Scenario: Queue navigation icon
- **WHEN** the Queue navigation item renders
- **THEN** it SHALL display a download/arrow-down icon

#### Scenario: History navigation icon
- **WHEN** the History navigation item renders
- **THEN** it SHALL display a clock/history icon

### Requirement: RuleSets route matching

The RuleSets navigation item SHALL be active for the `/rulesets` route and all nested routes (`/rulesets/:id`, `/rulesets/:id/history`, `/rulesets/:id/history/:requestId`).

#### Scenario: Nested route keeps parent active
- **WHEN** the user navigates to `/rulesets/tagesschau/history`
- **THEN** the RuleSets navigation item is active

### Requirement: Sidebar theme independence

The sidebar SHALL use `surface-raised` background in both light and dark themes. It does not change appearance when the theme changes.

#### Scenario: Light mode sidebar
- **WHEN** the user's system preference is light mode
- **THEN** the sidebar background remains `surface-raised` (dark)

### Requirement: Main content area

The main content area SHALL have a `surface-base` background. Content width constraints SHALL be set per-view, not globally. The main area SHALL NOT apply a fixed `max-w-*` class; instead, each view's root element sets its own width constraint.

#### Scenario: Content area rendering
- **WHEN** any page renders its content
- **THEN** the content is within the main area with `p-6` padding and no global max-width constraint

#### Scenario: Per-view width
- **WHEN** different views render
- **THEN** each view applies its own max-width class on its root element (or omits it for full-width views)

### Requirement: Breadcrumb bar

The main content area SHALL display a breadcrumb path above the page content for nested routes.

#### Scenario: Top-level route breadcrumb
- **WHEN** the user is on the Dashboard (`/`)
- **THEN** no breadcrumb is displayed

#### Scenario: Nested route breadcrumb
- **WHEN** the user is on `/rulesets/tagesschau`
- **THEN** a breadcrumb "RuleSets > tagesschau" is displayed above the page content
