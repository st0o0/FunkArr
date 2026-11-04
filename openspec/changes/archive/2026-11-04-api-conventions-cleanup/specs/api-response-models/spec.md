## MODIFIED Requirements

### Requirement: Date fields use DateTimeOffset
`DownloadHistoryItem.CompletedAt` SHALL be `DateTimeOffset` instead of `string`. `RuleSetListEntry.LastScoringRun` SHALL be `DateTimeOffset?` instead of `string?`. `CacheStatsResponse.OldestEntry` SHALL be `DateTimeOffset?` instead of `string?`. No manual `.ToString("o")` conversion SHALL occur in endpoint or mapping code.

#### Scenario: History item has DateTimeOffset
- **WHEN** the API returns a download history item
- **THEN** `completedAt` is serialized as an ISO 8601 DateTimeOffset by System.Text.Json

#### Scenario: Cache stats oldest entry is DateTimeOffset
- **WHEN** the API returns cache stats with a non-null oldest entry
- **THEN** `oldestEntry` is serialized as an ISO 8601 DateTimeOffset by System.Text.Json
- **AND** no `.ToString("o")` call SHALL exist in `SystemApiEndpoints`

#### Scenario: No manual date formatting in endpoints
- **WHEN** endpoint and mapping code is inspected
- **THEN** no `.ToString("o")` calls exist for date fields

## ADDED Requirements

### Requirement: FilterGroupTrace uses typed properties
`FilterGroupTrace.Operator` SHALL be `FilterOperator` (enum), not `string`. The mapping extension SHALL map from the domain's `FilterGroupOperator` to the API `FilterOperator` enum via exhaustive switch.

#### Scenario: Filter group trace operator is enum
- **WHEN** the API returns a scoring trace with filter group
- **THEN** `operator` SHALL be a `FilterOperator` enum value

### Requirement: FilterNodeTrace uses typed properties
`FilterNodeTrace.Field` SHALL be `FilterField?` (enum), not `string?`. `FilterNodeTrace.Op` SHALL be `FilterOp?` (enum), not `string?`. The mapping extension SHALL map from domain types via exhaustive switch.

#### Scenario: Filter node trace field is enum
- **WHEN** the API returns a scoring trace with filter node
- **THEN** `field` SHALL be a `FilterField` enum value and `op` SHALL be a `FilterOp` enum value

### Requirement: IdentificationTrace uses typed Strategy
`IdentificationTrace.Strategy` SHALL be `IdentificationStrategy?` (enum), not `string?`. The mapping extension SHALL map from the domain type via exhaustive switch. `IdentificationTrace.Detail` remains `string?` (freetext).

#### Scenario: Identification trace strategy is enum
- **WHEN** the API returns a scoring trace with identification info
- **THEN** `strategy` SHALL be an `IdentificationStrategy` enum value

#### Scenario: Identification trace detail remains string
- **WHEN** the API returns identification trace detail
- **THEN** `detail` SHALL remain `string?` (freetext, not an enum)
