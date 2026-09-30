## MODIFIED Requirements

### Requirement: RuleSet list presentation

The ruleset list view SHALL display ruleset entries as cards on `surface-raised` background with `border-default` borders. The ruleset ID SHALL render in `font-mono` with `brand-400` color. Topic text SHALL use `text-body`. Metadata (aliases, external IDs) SHALL use `text-secondary`.

#### Scenario: RuleSet card rendering
- **WHEN** the ruleset list loads with entries
- **THEN** each entry renders as a card with `surface-raised` background, `border-default` border, and `rounded-lg`

#### Scenario: RuleSet ID styling
- **WHEN** a ruleset card renders
- **THEN** the ruleset ID appears in `font-mono` with `brand-400` color

#### Scenario: Hover state
- **WHEN** the user hovers over a ruleset card
- **THEN** the border changes to `brand-500`

### Requirement: RuleSet detail layout

The ruleset detail view SHALL use cards with `surface-raised` background for each section (Identity, Source, Matching Rules). Section headings SHALL use `text-secondary` with `font-semibold`. Key-value grids SHALL use `text-secondary` for labels and `text-body` for values.

#### Scenario: Detail section rendering
- **WHEN** a ruleset detail loads
- **THEN** Identity, Source, and Matching Rules sections render as separate cards with `surface-raised` background

#### Scenario: Rule card styling
- **WHEN** matching rules render
- **THEN** each rule displays in a card with `surface-raised` background, rule ID in `font-mono brand-400`, and metadata in `text-secondary`

### Requirement: Scoring history table

The scoring history SHALL render as a table with `surface-elevated` header row, `text-secondary` uppercase column headers, and `surface-raised` data rows. Hover rows SHALL use `surface-elevated` background.

#### Scenario: Table header rendering
- **WHEN** the scoring history table renders
- **THEN** the header row has `surface-elevated` background with `text-secondary text-xs uppercase tracking-wider`

#### Scenario: Table row interaction
- **WHEN** the user hovers over a scoring history row
- **THEN** the row background changes to `surface-elevated`

### Requirement: Scoring detail trace styling

Scoring detail item traces SHALL use cards with colored left borders: `status-ok` for matched items and `border-default` for unmatched. Match/no-match badges SHALL use status colors on transparent backgrounds.

#### Scenario: Matched item rendering
- **WHEN** a scoring trace item has `matched: true`
- **THEN** the card has a left border in `status-ok` and a badge with `status-ok` text on `status-ok/10` background

#### Scenario: Unmatched item rendering
- **WHEN** a scoring trace item has `matched: false`
- **THEN** the card has a left border in `border-default` and a badge with `text-muted` on `surface-elevated` background

#### Scenario: Rule trace expansion
- **WHEN** the user expands a rule trace details section
- **THEN** trace entries display with colored left borders: `status-ok` for matched, `status-fail` for filterFailed, `border-default` for others
