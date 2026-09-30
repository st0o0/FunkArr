## MODIFIED Requirements

### Requirement: Dashboard page
The Vue frontend SHALL render an Overview page at route `/`. The page SHALL display a compact health status line, a download progress indicator, and a recent activity feed. It SHALL NOT display history stat cards, cache stats, or the previous 8-card stat wall.

#### Scenario: Overview renders
- **WHEN** the user navigates to `/`
- **THEN** the page displays health status, download progress, and recent activity feed

### Requirement: RuleSet list presentation
The ruleset list view SHALL display ruleset entries as cards on `surface-raised` background with `rounded-lg`. The ruleset ID SHALL render in `font-mono` with `text-text-secondary` color (not brand-400). Topic text SHALL use `text-text-body`. Source badge SHALL use `text-xs` with neutral styling. Section headings in the list view SHALL use normal case `text-sm font-semibold text-text-secondary` (not uppercase tracking-widest).

#### Scenario: RuleSet card rendering
- **WHEN** the ruleset list loads with entries
- **THEN** each entry renders as a card with `surface-raised` background and `rounded-lg`

#### Scenario: RuleSet ID styling
- **WHEN** a ruleset card renders
- **THEN** the ruleset ID appears in `font-mono` with `text-text-secondary` color

#### Scenario: Hover state
- **WHEN** the user hovers over a ruleset card
- **THEN** the card background SHALL change to `surface-elevated` via transition

### Requirement: Navigation and layout
The Vue frontend SHALL use a sidebar layout with flat navigation (no section headers). Navigation SHALL include links to Overview (`/`), Activity (`/activity`), and RuleSets (`/rulesets`). Setup SHALL be accessible via a gear icon at the sidebar bottom. Breadcrumb-style back navigation SHALL be available on detail pages.

#### Scenario: Navigation between pages
- **WHEN** the user clicks "RuleSets" in the navigation
- **THEN** the browser navigates to `/rulesets` without a full page reload

#### Scenario: Activity navigation
- **WHEN** the user clicks "Activity" in the navigation
- **THEN** the browser navigates to `/activity`

#### Scenario: Breadcrumb on detail page
- **WHEN** the user is on `/rulesets/tatort`
- **THEN** breadcrumbs show "RuleSets > tatort" with "RuleSets" linking back to the list

### Requirement: Navigation updates
The sidebar navigation SHALL include Overview (`/`), Activity (`/activity`), RuleSets (`/rulesets`) as main nav items. Setup SHALL be a gear icon at the sidebar bottom. The builder page SHALL be accessible via the "New RuleSet" button on the list page and "Edit" button on the detail page.

#### Scenario: Builder breadcrumb on create
- **WHEN** the user is on `/rulesets/new`
- **THEN** breadcrumbs show "RuleSets > New"

#### Scenario: Builder breadcrumb on edit
- **WHEN** the user is on `/rulesets/tatort/edit`
- **THEN** breadcrumbs show "RuleSets > tatort > Edit" with "RuleSets" and "tatort" as links

### Requirement: Scoring history table

The scoring history SHALL render as a table with `surface-raised` header row, `text-text-secondary` column headers in normal case `text-xs font-medium` (not uppercase tracking-wider), and `surface-raised` data rows.

#### Scenario: Table header rendering
- **WHEN** the scoring history table renders
- **THEN** the header row has `surface-raised` background with `text-text-secondary text-xs font-medium` in normal case

#### Scenario: Table row interaction
- **WHEN** the user hovers over a scoring history row
- **THEN** the row background changes to `surface-elevated`
