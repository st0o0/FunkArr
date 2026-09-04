## Purpose

Three-level card hierarchy establishing visual depth and interaction patterns across the FunkArr UI: section cards, list item cards, and table rows.

## Requirements

### Requirement: Level 1 section card
Level 1 "section cards" SHALL use classes `bg-surface-raised rounded-xl border border-border-default`. They are used for dashboard widgets (HealthWidget, ActiveDownloads) and standalone content sections (Setup wizard steps, RuleSet detail sections). Section cards MAY have a header bar separated by `border-b border-border-subtle`.

#### Scenario: Dashboard widget uses section card
- **WHEN** the HealthWidget or ActiveDownloads component renders
- **THEN** the outer container SHALL use Level 1 section card classes

#### Scenario: Section card with header
- **WHEN** a section card includes a header bar
- **THEN** the header SHALL be separated from the body by `border-b border-border-subtle` with `px-5 py-3.5` padding

### Requirement: Level 2 list item card
Level 2 "list item" cards SHALL use classes `bg-surface-raised rounded-lg transition-all duration-200 ease-out hover:-translate-y-px hover:shadow-md`. They MUST NOT have a visible border in their default state. They are used for RuleSet list items and Queue cards.

#### Scenario: RuleSet list item uses list item card
- **WHEN** the RuleSet list renders a ruleset entry
- **THEN** the entry SHALL use Level 2 list item card classes without a visible border

#### Scenario: Queue card uses list item card
- **WHEN** the Queue view renders a QueueCard
- **THEN** the QueueCard container SHALL use Level 2 list item card classes

### Requirement: Level 3 table row
Level 3 "table rows" SHALL have no explicit background color. Clickable rows SHALL use `hover:bg-surface-elevated/60 cursor-pointer transition-colors duration-150`. Non-clickable rows SHALL use `hover:bg-surface-elevated/30 transition-colors duration-150`. Row separators use `border-b border-border-subtle`.

#### Scenario: History table row hover
- **WHEN** the user hovers over a row in the History table
- **THEN** the row SHALL show `bg-surface-elevated/60` background because History rows are clickable (they aren't currently, but Scoring rows are)

#### Scenario: Non-clickable table row hover
- **WHEN** the user hovers over a non-clickable table row
- **THEN** the row SHALL show a subtler `bg-surface-elevated/30` background

### Requirement: Primary button variant
Primary buttons SHALL use classes `bg-brand-600 text-white rounded-lg hover:bg-brand-500 active:scale-[0.98] transition-all duration-150`. Used for main actions (Save, Next, Search, Test).

#### Scenario: Primary button interaction
- **WHEN** the user hovers over a primary button
- **THEN** the background SHALL lighten to brand-500
- **WHEN** the user presses the button
- **THEN** the button SHALL scale to 98%

### Requirement: Secondary button variant
Secondary buttons SHALL use classes `bg-surface-elevated border border-border-default rounded-lg text-text-body hover:border-brand-500/40 hover:bg-surface-elevated/80 active:scale-[0.98] transition-all duration-150`. Used for secondary actions (Back, Cancel, Re-check).

#### Scenario: Secondary button interaction
- **WHEN** the user hovers over a secondary button
- **THEN** the border SHALL gain a brand-colored tint

### Requirement: Ghost button variant
Ghost buttons SHALL use classes `text-brand-400 hover:text-brand-300 hover:bg-brand-900/10 rounded-lg active:scale-[0.98] transition-all duration-150`. Used for tertiary actions and in-context links (View Queue, Setup Guide).

#### Scenario: Ghost button interaction
- **WHEN** the user hovers over a ghost button
- **THEN** the text SHALL lighten and a subtle brand-tinted background SHALL appear

### Requirement: Danger button variant
Danger buttons SHALL use classes `bg-status-fail/10 text-status-fail border border-status-fail/20 rounded-lg hover:bg-status-fail/20 active:scale-[0.98] transition-all duration-150`. Used for destructive actions (Delete, Confirm delete).

#### Scenario: Danger button interaction
- **WHEN** the user hovers over a danger button
- **THEN** the red background SHALL intensify from 10% to 20% opacity
