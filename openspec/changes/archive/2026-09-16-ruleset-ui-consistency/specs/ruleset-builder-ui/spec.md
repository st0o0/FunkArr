## MODIFIED Requirements

### Requirement: Filter builder
Each rule editor SHALL include a filter builder with three sections: ALL (all conditions must match), ANY (at least one must match), and NOT (none may match). Section headers SHALL display localized group labels from the shared `groupLabel()` utility instead of raw English strings. Each condition SHALL have a field dropdown showing localized field labels from `fieldLabel()` (with enum values preserved as option values), an operator dropdown showing localized operator labels from `opLabel()` (with enum values preserved as option values), and a value text input. The builder SHALL support adding and removing conditions within each section.

#### Scenario: Localized group headers
- **WHEN** the filter builder renders with DE locale
- **THEN** section headers display localized labels (e.g. "Alle erfüllt", "Mind. eins", "Keines") instead of "all", "any", "not"

#### Scenario: Localized op dropdown
- **WHEN** the operator dropdown renders with DE locale
- **THEN** options display localized labels (e.g. "größer als", "enthält", "gleich") while option values remain enum strings ("greaterThan", "contains", "eq")

#### Scenario: Localized field dropdown
- **WHEN** the field dropdown renders with DE locale
- **THEN** options display localized labels (e.g. "Titel", "Thema", "Dauer") while option values remain enum strings ("title", "topic", "duration")

#### Scenario: Add filter condition
- **WHEN** the user adds a condition to any section
- **THEN** the condition appears with localized dropdowns

#### Scenario: Remove filter condition
- **WHEN** the user clicks remove on a filter condition
- **THEN** the condition is removed from its section

### Requirement: Title rules builder
When a TitleConstruction strategy is selected (itemTitleExact or itemTitleIncludes), the rule editor SHALL show a title rules builder. Type picker options SHALL use localized labels from the shared `titlePartLabel()` utility. Field dropdown options for regex parts SHALL use localized labels from `fieldLabel()`. All option values SHALL remain as enum strings for serialization.

#### Scenario: Localized type picker
- **WHEN** the title part type picker renders with DE locale
- **THEN** options display localized labels (e.g. "Statisch", "Regex") while values remain "static", "regex"

#### Scenario: Localized field picker in regex part
- **WHEN** a regex title part's field dropdown renders with DE locale
- **THEN** options display localized labels (e.g. "Titel", "Thema") while values remain "title", "topic"
