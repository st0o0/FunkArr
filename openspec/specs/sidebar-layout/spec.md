## Purpose

Application shell layout with fixed sidebar navigation, brand wordmark, responsive collapse, and breadcrumb bar for the main content area.

## Requirements

### Requirement: Sidebar navigation structure

The application layout SHALL use a CSS Grid with a fixed-width sidebar (64px) and fluid main content area. The sidebar SHALL contain the FunkArr wordmark, navigation links, and a version footer.

#### Scenario: Desktop layout rendering
- **WHEN** the viewport is 768px or wider
- **THEN** the layout renders as a two-column grid with a 64px sidebar on the left and the content area filling the remaining width

#### Scenario: Sidebar sections
- **WHEN** the sidebar renders
- **THEN** it contains three sections top-to-bottom: brand wordmark, navigation links, version number

### Requirement: Sidebar brand wordmark

The sidebar SHALL display "FunkArr" as a text wordmark using the `brand-500` color, bold weight, and tight tracking.

#### Scenario: Wordmark rendering
- **WHEN** the sidebar renders
- **THEN** the wordmark "FunkArr" appears at the top in `brand-500` color with `font-bold` and `tracking-tight`

### Requirement: Navigation items

The sidebar SHALL contain five navigation items in order: Dashboard (`/`), Queue (`/queue`), History (`/history`), RuleSets (`/rulesets`), and Setup (`/setup`). Each item SHALL show an icon and label.

#### Scenario: Active route indication
- **WHEN** the current route matches a navigation item
- **THEN** that item displays a left border in `brand-500` and a background tint of `brand-900/20`, with `text-primary` color

#### Scenario: Inactive route styling
- **WHEN** the current route does not match a navigation item
- **THEN** that item uses `text-secondary` color with no left border

#### Scenario: Hover state
- **WHEN** the user hovers over an inactive navigation item
- **THEN** the item background changes to `surface-elevated` and text changes to `text-body`

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

### Requirement: Responsive sidebar collapse

Below 768px viewport width, the sidebar SHALL collapse to 48px width. Navigation labels SHALL be hidden (screen-reader accessible via `sr-only`). Icons remain visible.

#### Scenario: Narrow viewport collapse
- **WHEN** the viewport width is below 768px
- **THEN** the sidebar width is 48px and navigation labels are visually hidden

#### Scenario: Labels remain accessible
- **WHEN** the sidebar is collapsed
- **THEN** navigation labels are present in the DOM with `sr-only` class for screen readers

### Requirement: Sidebar theme independence

The sidebar SHALL use `surface-raised` background in both light and dark themes. It does not change appearance when the theme changes.

#### Scenario: Light mode sidebar
- **WHEN** the user's system preference is light mode
- **THEN** the sidebar background remains `surface-raised` (dark)

### Requirement: Main content area

The main content area SHALL have a `surface-base` background, with content constrained to `max-w-7xl` and centered horizontally with padding.

#### Scenario: Content area rendering
- **WHEN** any page renders its content
- **THEN** the content is within a container with `max-w-7xl`, horizontally centered, with `p-6` padding

### Requirement: Breadcrumb bar

The main content area SHALL display a breadcrumb path above the page content for nested routes.

#### Scenario: Top-level route breadcrumb
- **WHEN** the user is on the Dashboard (`/`)
- **THEN** no breadcrumb is displayed

#### Scenario: Nested route breadcrumb
- **WHEN** the user is on `/rulesets/tagesschau`
- **THEN** a breadcrumb "RuleSets > tagesschau" is displayed above the page content
