## MODIFIED Requirements

### Requirement: Filter trace rendering
The `FilterGroupTraceView` component SHALL use the shared `opSymbol()` function to display compact operator symbols instead of raw enum names in filter condition traces. The component SHALL use the shared `groupLabel()` function to display localized group headers instead of the raw `group.operator` string from the API.

#### Scenario: Op symbols in trace
- **WHEN** a filter trace shows a condition with op "greaterThan"
- **THEN** the condition displays `>` instead of `greaterThan`

#### Scenario: Localized group header in trace
- **WHEN** a filter trace group has operator "all" and locale is DE
- **THEN** the group header displays the localized label instead of raw "all"

#### Scenario: Field labels in trace
- **WHEN** a filter trace shows a condition with field "duration"
- **THEN** the condition displays the localized field label instead of raw "duration"

#### Scenario: Trace coloring preserved
- **WHEN** filter trace conditions render with shared vocabulary
- **THEN** pass/fail coloring and skip states SHALL be preserved unchanged
