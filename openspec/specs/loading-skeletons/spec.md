## Purpose

Skeleton loading components (SkeletonLine, SkeletonCard, SkeletonTable) with shimmer animation replacing "Loading..." text across all views.

## Requirements

### Requirement: SkeletonLine component
A `SkeletonLine` component SHALL render a single animated bar that mimics a line of text or a data field.
It MUST accept an optional `width` prop (e.g., `w-1/2`, `w-full`) to control the bar's width.

#### Scenario: Rendering a skeleton line
- **WHEN** `<SkeletonLine />` is rendered
- **THEN** it MUST display a horizontal bar with the shimmer animation
- **AND** the bar height MUST approximate a single line of text

#### Scenario: Custom width
- **WHEN** `<SkeletonLine width="w-2/3" />` is rendered
- **THEN** the bar MUST span approximately two-thirds of the container width

### Requirement: SkeletonCard component
A `SkeletonCard` component SHALL render a card-shaped placeholder that mimics the layout of a content card.

#### Scenario: Rendering a skeleton card
- **WHEN** `<SkeletonCard />` is rendered
- **THEN** it MUST display a rounded rectangle matching the dimensions and border-radius of a section card
- **AND** the shimmer animation MUST be active

### Requirement: SkeletonTable component
A `SkeletonTable` component SHALL render a table-shaped placeholder with configurable rows and columns.
It MUST accept `rows` (default: 5) and `columns` (default: 4) props.

#### Scenario: Rendering a skeleton table
- **WHEN** `<SkeletonTable :rows="5" :columns="4" />` is rendered
- **THEN** it MUST display a header row and 5 body rows, each with 4 column placeholders
- **AND** each cell MUST contain a shimmer-animated bar

#### Scenario: Custom row and column count
- **WHEN** `<SkeletonTable :rows="3" :columns="6" />` is rendered
- **THEN** the table MUST have 3 body rows and 6 columns

### Requirement: Shimmer animation
All skeleton components MUST use the same shimmer animation: a CSS gradient that sweeps from left to right across the skeleton surface.

#### Scenario: Shimmer visual
- **WHEN** any skeleton component is visible
- **THEN** a highlight gradient MUST move continuously from left to right across the component
- **AND** the animation MUST loop indefinitely until the component is unmounted

#### Scenario: Shimmer uses surface tokens
- **WHEN** the shimmer animation renders
- **THEN** the base color MUST use `surface-elevated`
- **AND** the highlight sweep MUST use a lighter shade of the surface color (not white)

### Requirement: All views replace text loading indicators
Every view that currently displays "Loading..." text SHALL replace it with an appropriate skeleton composition that matches the shape of the view's real content.

#### Scenario: Queue view loading
- **WHEN** the Queue view is loading data
- **THEN** it MUST display skeleton cards matching the approximate layout of QueueCard components

#### Scenario: History view loading
- **WHEN** the History view is loading data
- **THEN** it MUST display a SkeletonTable matching the column count and approximate row count of the history table

#### Scenario: RuleSet list loading
- **WHEN** the RuleSet list view is loading data
- **THEN** it MUST display skeleton cards matching the approximate layout of ruleset list items

#### Scenario: Health widget loading
- **WHEN** the HealthWidget is loading health check data
- **THEN** it MUST display skeleton lines inside the widget matching the approximate layout of health check rows

#### Scenario: Setup view loading
- **WHEN** the Setup view is running the initial health check
- **THEN** it MUST display skeleton lines matching the approximate layout of health check result items
