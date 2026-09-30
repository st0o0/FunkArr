## MODIFIED Requirements

### Requirement: Debugger presentation

Result candidate cards SHALL use Level 1 card styling (`surface-raised` background with `border-default` borders). Matched cards SHALL have a 4px left border in `status-ok`. Unmatched cards SHALL have a 4px left border in `border-default`. Individual result items within the results list SHALL use Level 2 card styling with hover effects for expand/collapse interaction. Filter condition rows SHALL alternate between `surface-base` and `surface-raised` backgrounds. The rule pipeline trace SHALL use indented sections with `border-l-2` left borders colored by outcome.

#### Scenario: Matched card styling
- **WHEN** a matched candidate card renders
- **THEN** it has a `status-ok` 4px left border, green match badge, and Level 2 hover effect on the header area

#### Scenario: Unmatched card styling
- **WHEN** an unmatched candidate card renders
- **THEN** it has a `border-default` 4px left border and gray badge

#### Scenario: Trace indentation
- **WHEN** a rule pipeline trace is expanded
- **THEN** filter groups are indented under the rule, and conditions are indented under the group

#### Scenario: Result card expand interaction
- **WHEN** the user hovers over a result card header
- **THEN** the header area shows a subtle background change indicating it is clickable
