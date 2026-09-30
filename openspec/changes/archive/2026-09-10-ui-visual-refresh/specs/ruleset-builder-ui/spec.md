## MODIFIED Requirements

### Requirement: Builder presentation
The builder form SHALL use `surface-raised` cards for each section (Identity, Rules). Form inputs SHALL use `surface-elevated` backgrounds with `border-default` borders. Section headings SHALL use `text-sm font-semibold text-text-secondary` in normal case (not uppercase tracking-widest). The Save button SHALL use Primary button tier styling (amber accent background, black text). The Cancel button SHALL use Secondary button tier styling.

#### Scenario: Form section rendering
- **WHEN** the builder form renders
- **THEN** Identity and Rules sections are separate `surface-raised` cards

#### Scenario: Section heading styling
- **WHEN** a section heading renders (e.g., "Identity", "Matching Rules")
- **THEN** it SHALL use `text-sm font-semibold text-text-secondary` in normal case
- **AND** SHALL NOT use `uppercase tracking-widest` or `tracking-wider`

#### Scenario: Save button primary tier
- **WHEN** the Save button renders
- **THEN** it SHALL use Primary button tier: `bg-accent text-black font-medium rounded-md`

#### Scenario: Cancel button secondary tier
- **WHEN** the Cancel button renders
- **THEN** it SHALL use Secondary button tier: `bg-surface-elevated border border-border-default text-text-body rounded-md`

### Requirement: RuleSet builder page
The Vue frontend SHALL render a ruleset builder page at route `/rulesets/new` for creating new rulesets and `/rulesets/:id/edit` for editing existing ones. The page SHALL use an asymmetric split-pane layout: the builder form on the left (wider) and the search + test panel on the right (narrower). The grid SHALL use `grid-cols-[1fr_380px]` within a `max-w-5xl mx-auto` container.

#### Scenario: Navigate to create new ruleset
- **WHEN** the user navigates to `/rulesets/new`
- **THEN** the builder form renders with empty fields and the search panel on the right

#### Scenario: Navigate to edit existing ruleset
- **WHEN** the user navigates to `/rulesets/tatort/edit`
- **THEN** the builder form is populated with the existing ruleset data from the API

#### Scenario: Asymmetric split-pane layout
- **WHEN** the builder page renders
- **THEN** the builder form occupies the left pane (fluid, `1fr`) and the search/test panel occupies the right pane (fixed `380px`)
- **AND** the entire layout is constrained to `max-w-5xl mx-auto`
