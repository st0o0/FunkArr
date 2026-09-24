## Purpose

Shared on-disk record types and bidirectional mapping extensions between disk JSON format and domain types. All enum fields use `string?` on disk; conversion uses `RuleSetEnumMapping`.

## Requirements

### Requirement: DiskRuleSet records represent on-disk format
The `DiskModel/` directory SHALL contain sealed record types that mirror the on-disk JSON structure: `DiskRuleSet`, `DiskRule`, `DiskMedia`, `DiskFilterGroup`, `DiskFilter`, `DiskTitleRule`, `DiskEnrichment` (and sub-records). All enum fields SHALL be `string?` (the on-disk representation). Records SHALL be `internal` to `FunkArr.RuleSet`.

#### Scenario: DiskRuleSet structure
- **WHEN** the disk model is inspected
- **THEN** `DiskRuleSet` SHALL have properties: `Topic`, `Aliases`, `Media`, `Confidence`, `Rules`, `Standalone`, `Disable`, `Enrichment`

#### Scenario: Strategy is string on disk
- **WHEN** a `DiskRule` is inspected
- **THEN** `Strategy` SHALL be `string?`, not an enum type

### Requirement: DiskJsonOptions provides shared serializer
A single `DiskJsonOptions.Default` instance SHALL be used for all disk serialization and deserialization. It SHALL use `CamelCase` naming, `WhenWritingNull` ignore, `WriteIndented`, and `JsonStringEnumConverter` for non-strategy enums (FilterField, FilterOp, TitlePartType, EnrichmentMethod, RuntimeMode, MediaType).

#### Scenario: Single options instance
- **WHEN** `DiskJsonOptions.Default` is used for deserialization and serialization
- **THEN** both directions SHALL produce identical results (roundtrip-safe)

### Requirement: DiskToMatchingExtensions maps disk to domain
Extension methods on `DiskRuleSet` SHALL convert to domain types using `RuleSetEnumMapping` for enum conversion.

#### Scenario: ToMatchingConfig
- **WHEN** `disk.ToMatchingConfig("tatort")` is called on a merged DiskRuleSet
- **THEN** the result SHALL be a `MatchingConfig` with rules transformed to `MatchingRule[]`

#### Scenario: ToIdentity
- **WHEN** `disk.ToIdentity()` is called
- **THEN** the result SHALL contain `Topic`, `Aliases`, `TvdbId`, `ImdbId`, `TmdbId`, `MediaName`, `MediaType`, `EnrichmentConfig`

#### Scenario: ToDetailRules
- **WHEN** `disk.Rules.ToDetailRules()` is called
- **THEN** the result SHALL be `RuleSetDetailRule[]` with structured filters and title rules

### Requirement: MatchingToDiskExtensions maps domain to disk
Extension methods on `RuleSetBody` SHALL convert to `DiskRuleSet` using `RuleSetEnumMapping.ToDiskValue()` for all enum fields.

#### Scenario: ToDiskRuleSet with strategy
- **WHEN** a body with `IdentificationStrategy.TitleIncludes` is converted
- **THEN** the resulting `DiskRule.Strategy` SHALL be `"itemTitleIncludes"`

#### Scenario: ToDiskRuleSet with filters
- **WHEN** a body with `FilterField.Title` and `FilterOp.Contains` is converted
- **THEN** the resulting disk filter SHALL have `Field: "title"` and `Op: "contains"`
