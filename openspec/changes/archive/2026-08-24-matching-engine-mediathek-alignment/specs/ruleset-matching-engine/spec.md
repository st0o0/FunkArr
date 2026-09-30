## ADDED Requirements

### Requirement: Composite topicTitle field

The matching engine SHALL support `"topicTitle"` as a valid field name in `GetFieldValue`, returning the item's topic and title concatenated as `"{Topic}: {Title}"`. This field SHALL be usable in TitleRule `field` references and Filter `field` references.

#### Scenario: topicTitle field resolution
- **WHEN** a TitleRule references `field: "topicTitle"` for a Mediathek item with `topic = "Tatort"` and `title = "Die goldene Zeit"`
- **THEN** `GetFieldValue` SHALL return `"Tatort: Die goldene Zeit"`

#### Scenario: topicTitle in filter
- **WHEN** a Filter uses `field: "topicTitle"` with `op: "contains"` and `value: "Tatort"`
- **THEN** the filter SHALL match items where the combined topic+title contains "Tatort"

#### Scenario: existing fields unchanged
- **WHEN** a TitleRule references `field: "title"`
- **THEN** `GetFieldValue` SHALL return `item.Title` only, unchanged from current behavior

## MODIFIED Requirements

### Requirement: BuildTitle separator trimming

The `BuildTitle` method SHALL trim leading and trailing separator characters (whitespace, dashes, colons) from the constructed title string before returning it. This ensures that static TitleRule separators at the start or end of the concatenated result do not interfere with TVDB episode name comparison.

#### Scenario: Leading static separator trimmed
- **WHEN** TitleRules produce `[Static(" - "), Regex("Die goldene Zeit")]`
- **THEN** `BuildTitle` SHALL return `"Die goldene Zeit"` (not `" - Die goldene Zeit"`)

#### Scenario: Trailing static separator trimmed
- **WHEN** TitleRules produce `[Regex("Die goldene Zeit"), Static(" - ")]`
- **THEN** `BuildTitle` SHALL return `"Die goldene Zeit"` (not `"Die goldene Zeit - "`)

#### Scenario: Interior separators preserved
- **WHEN** TitleRules produce `[Regex("Teil 1"), Static(" - "), Regex("Die Flucht")]`
- **THEN** `BuildTitle` SHALL return `"Teil 1 - Die Flucht"` (interior separator preserved)

#### Scenario: Null result unchanged
- **WHEN** a regex TitleRule fails to match
- **THEN** `BuildTitle` SHALL still return `null` as before
