## Why

Audit found ~40 enums violating the project's enum conventions: most Messages and Api enums lack explicit int values and `Unknown = 0`. Only Persistence (10/11) and ArrApi (3/3) are compliant. Implicit int values risk silent breaking changes on member reorder, and missing `Unknown = 0` means `default(T)` is indistinguishable from a valid value.

## What Changes

- Add explicit int values to every enum member across all projects (not just Persistence)
- Add `Unknown = 0` to enums that cross wire boundaries (Api, Messages) with documented exemptions
- Fix `PersistedSearchSource` — only partial explicit values (`Feed = 4`, rest implicit)
- Fix `SourceType` in Api — has `Unknown` but as last member (implicit = 3), move to first with `= 0`
- Update all enum mapping switch expressions to handle new `Unknown` members
- Update Api handlers to reject `Unknown` values before mapping

### Exemptions from `Unknown = 0`

- **Priority enums** (`DownloadPriority`, `QueuePriority`): Semantic negative values (`Low = -1, Normal = 0, High = 1`), always explicitly set
- **FailureReason enums** (4x in `LocalRuleSetCommands.cs`): 1-2 members, failure reasons are always specific
- **Actor-internal enums** (`SearchType` nested in `SearchManager`): Never serialized or crosses boundaries
- **Internal enums** (`SubtitleFailureReason`): Domain-internal, never on wire

## Capabilities

### New Capabilities

_None — this is a convention enforcement pass, not a new feature._

### Modified Capabilities

- `domain-enums`: Add explicit int values to all domain-project enums (`CrawlStatus`, `VariantPolicy`)
- `persistence-enum-values`: Fix `PersistedSearchSource` partial explicit values
- `enum-mapping-switch`: Add `Unknown` handling arms to all mapping switches
- `api-enum-types`: Add `Unknown = 0` and explicit ints to all Api enums, fix `SourceType` ordering

## Impact

- **FunkArr.Persistence**: 1 enum (`PersistedSearchSource`)
- **FunkArr.Messages**: 18 enums — explicit ints + `Unknown = 0` where applicable
- **FunkArr.Api**: 18 enums — `Unknown = 0` + explicit ints; mapping extensions; endpoint handlers for `Unknown` rejection
- **FunkArr.Api.Tests**: Test updates for new `Unknown` handling and mapping changes
- **Domain projects**: 4 enums (Feed, Core, Search, Download) — explicit ints + `Unknown = 0` where applicable
- **No ArrApi changes** — already compliant
- **No breaking wire changes** — existing enum values keep their numeric assignments, `Unknown` is additive
