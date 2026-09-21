## Purpose

Shared utilities and i18n keys for consistent rendering of filter operators, fields, groups, and title part types across all RuleSet-related frontend views.

## Requirements

### Requirement: Filter operator symbols
The frontend SHALL provide a function `opSymbol(op: string): string` that maps filter operator enum values to compact display symbols: `eq` → `=`, `contains` → `∋`, `notContains` → `∌`, `greaterThan` → `>`, `lessThan` → `<`, `regex` → `≈`. Unknown values SHALL fall back to the raw string.

#### Scenario: Known operator symbol
- **WHEN** `opSymbol("greaterThan")` is called
- **THEN** the function returns `">"`

#### Scenario: Unknown operator symbol
- **WHEN** `opSymbol("customOp")` is called
- **THEN** the function returns `"customOp"`

### Requirement: Filter operator labels
The frontend SHALL provide a function `opLabel(op: string, t): string` that maps filter operator enum values to localized display labels via i18n keys under `rule.op.*`. All six operators SHALL have DE and EN translations.

#### Scenario: Operator label in German
- **WHEN** `opLabel("contains", t)` is called with DE locale
- **THEN** the function returns the German translation (e.g. "enthält")

#### Scenario: Operator label in English
- **WHEN** `opLabel("greaterThan", t)` is called with EN locale
- **THEN** the function returns "greater than"

### Requirement: Filter group labels
The frontend SHALL provide a function `groupLabel(group: string, t): string` that maps filter group names to localized labels via i18n keys under `rule.group.*`: `all` → "alle erfüllt"/"all match", `any` → "mind. eins"/"any matches", `not` → "keines"/"none match".

#### Scenario: Group label in German
- **WHEN** `groupLabel("all", t)` is called with DE locale
- **THEN** the function returns the German translation (e.g. "Alle erfüllt")

#### Scenario: Unknown group fallback
- **WHEN** `groupLabel("custom", t)` is called
- **THEN** the function returns `"custom"`

### Requirement: Field labels
The frontend SHALL provide a function `fieldLabel(field: string, t): string` that maps filter/title-rule field enum values to localized labels via i18n keys under `rule.field.*`: `title`, `topic`, `channel`, `description`, `duration`, `timestamp`. All fields SHALL have DE and EN translations.

#### Scenario: Field label in German
- **WHEN** `fieldLabel("duration", t)` is called with DE locale
- **THEN** the function returns "Dauer"

#### Scenario: Field label in English
- **WHEN** `fieldLabel("channel", t)` is called with EN locale
- **THEN** the function returns "Channel"

### Requirement: Title part type labels
The frontend SHALL provide a function `titlePartLabel(type: string, t): string` that maps title part type enum values to localized labels via i18n keys under `rule.titlePart.*`: `static` and `regex`.

#### Scenario: Title part label in German
- **WHEN** `titlePartLabel("static", t)` is called with DE locale
- **THEN** the function returns the German translation (e.g. "Statisch")

### Requirement: i18n key namespace
All rule vocabulary i18n keys SHALL be organized under a `rule` top-level namespace in both DE and EN locale files. The namespace SHALL contain sub-objects: `op` (operator labels), `opSymbol` (not needed - symbols are not locale-dependent), `group` (group labels), `field` (field labels), `titlePart` (title part type labels).

#### Scenario: i18n structure
- **WHEN** the EN locale file is inspected
- **THEN** it SHALL contain `rule.op.eq`, `rule.op.contains`, `rule.op.notContains`, `rule.op.greaterThan`, `rule.op.lessThan`, `rule.op.regex`, `rule.group.all`, `rule.group.any`, `rule.group.not`, `rule.field.title`, `rule.field.topic`, `rule.field.channel`, `rule.field.description`, `rule.field.duration`, `rule.field.timestamp`, `rule.titlePart.static`, `rule.titlePart.regex`

### Requirement: Filter condition display component
The frontend SHALL provide a `FilterConditionDisplay.vue` component that renders a `FilterGroupOutput` as visually grouped conditions. Each non-empty group (`all`/`any`/`not`) SHALL render as a container with a localized header (via `groupLabel()`) and condition rows showing `fieldLabel opSymbol value` per condition. Empty groups SHALL not be rendered.

#### Scenario: Render single group
- **WHEN** a filter has `all: [{ field: "duration", op: "greaterThan", value: "35" }]`
- **THEN** the component renders a container with header "Alle erfüllt" (DE) and one row: "Dauer > 35"

#### Scenario: Render multiple groups
- **WHEN** a filter has conditions in `all` and `not`
- **THEN** both groups render as separate containers with their respective headers

#### Scenario: Empty filter
- **WHEN** filters is null
- **THEN** nothing is rendered

### Requirement: Title rule display component
The frontend SHALL provide a `TitleRuleDisplay.vue` component that renders a `TitleRuleOutput[]` as an ordered list of title construction parts. Each part SHALL show its type as a small badge. Static parts SHALL display their value in quotes. Regex parts SHALL display field label, `→`, and the pattern in monospace, with capture group if present.

#### Scenario: Static title part
- **WHEN** a title rule has `{ type: "static", value: "-" }`
- **THEN** it renders as badge `static` followed by `"-"`

#### Scenario: Regex title part
- **WHEN** a title rule has `{ type: "regex", field: "title", pattern: "(?<=Tatort[:\\s*])(.*)$", captureGroup: 1 }`
- **THEN** it renders as badge `regex` followed by field label `→` `/pattern/` and capture group indicator

#### Scenario: Empty title rules
- **WHEN** titleRules is null or empty
- **THEN** nothing is rendered
