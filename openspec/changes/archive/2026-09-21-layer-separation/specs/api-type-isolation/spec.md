## ADDED Requirements

### Requirement: Api.Models owns all enum types used in API responses
FunkArr.Api.Models SHALL define its own copy of every enum used as a field type in Api.Models records. These MUST NOT reference Messages enum types directly. Each Api enum copy SHALL carry the appropriate `[JsonStringEnumConverter]` and `[JsonStringEnumMemberName]` attributes for System.Text.Json serialization.

#### Scenario: Api MediaType enum
- **WHEN** `DownloadQueueItem.Category`, `DownloadHistoryItem.Category`, or `RuleSetListEntry.MediaType` is defined
- **THEN** the field type is an Api-owned `MediaType` enum, not `Messages.MediaType`

#### Scenario: Api SearchSource enum
- **WHEN** `ScoringDetail.Source` or `ScoringSnapshotSummary.Source` is defined
- **THEN** the field type is an Api-owned `SearchSource` enum, not `Messages.SearchSource`

#### Scenario: Api Scoring enums
- **WHEN** `FilterNodeInput`, `TitleRuleInput`, `RuleSetDetailRule`, or `RuleInput` use `FilterOp`, `FilterField`, `IdentificationStrategy`, or `TitlePartType`
- **THEN** these are Api-owned enum copies, not Messages types

#### Scenario: Api Enrichment enums
- **WHEN** `EnrichmentConfigOutput`, `RuntimeMatchConfigOutput`, or `EnrichmentTraceOutput` use `EnrichmentMethod`, `RuntimeMode`, or `MatchMethod`
- **THEN** these are Api-owned enum copies, not Messages types

#### Scenario: Json string serialization on Api enums
- **WHEN** any Api enum is serialized to JSON
- **THEN** it produces string values (not integers) matching the current API contract

### Requirement: Api.Models owns all complex types used in API responses
FunkArr.Api.Models SHALL NOT use Messages complex record types directly as field types. `FilterGroupOutput` and `TitleRuleOutput` from Messages MUST be replaced with Api-owned copies.

#### Scenario: RuleSetDetailRule uses Api-owned types
- **WHEN** `RuleSetDetailRule` is defined
- **THEN** its filter and title rule fields use Api-owned record types, not `Messages.Scoring.FilterGroupOutput` or `Messages.Scoring.TitleRuleOutput`

### Requirement: ToApi/FromApi mapping for all Api types
Every Api.Models type that corresponds to a Messages type SHALL be connected via explicit `ToApi()` and/or `FromApi()` extension methods. No inline field-by-field construction in endpoint code.

#### Scenario: Consistent mapping pattern
- **WHEN** an API endpoint converts a Messages response to an Api.Models type
- **THEN** it uses a `.ToApi()` extension method, not inline construction

#### Scenario: Bidirectional mapping for input types
- **WHEN** an API endpoint receives input and converts to a Messages command
- **THEN** it uses a `.ToMessage()` or `.FromApi()` extension method
