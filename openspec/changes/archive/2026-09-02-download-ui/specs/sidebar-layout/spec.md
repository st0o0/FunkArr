# Sidebar Layout

## MODIFIED Requirements

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
