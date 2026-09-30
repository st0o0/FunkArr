## Why

The API endpoints for ruleset CRUD (create, update, delete, export) directly perform disk I/O, JSON serialization with a separate `_diskJsonOptions`, and schema validation. This mixes the API layer (number enums) with the disk format (string enums), causing serialization bugs: the API accepts number enums from the frontend but the JSON schema requires string enums on disk. Moving write operations behind a dedicated actor separates these concerns cleanly and aligns with the existing Akka actor architecture.

## What Changes

- Rename `RuleSetMerger` → `RuleSetReader` for symmetry with the new writer
- Extract `RuleSetEnumMapping` from the reader's `TryParseStrategy` switch — bidirectional enum↔string conversion used by both reader and writer
- New `RuleSetWriter` service class in `FunkArr.RuleSet` — converts domain messages to disk-format JSON (string enums), validates against schema, serializes
- New `LocalRuleSetWriter` actor in `FunkArr.RuleSet` — receives Create/Update/Delete commands via Ask, delegates to `RuleSetWriter`, writes to disk via `IDataFiles`
- New command/response messages in `FunkArr.Messages` — `CreateLocalRuleSet`, `UpdateLocalRuleSet`, `DeleteLocalRuleSet` with typed response hierarchies
- API endpoint cleanup — `HandleCreate`/`HandleUpdate`/`HandleDelete`/`HandleExport` become Ask calls to `LocalRuleSetWriter`; remove `_diskJsonOptions`, `SerializeForDisk`, direct `IDataFiles`/`IRuleSetValidator` injection
- Remove `JsonStringEnumMemberName` attributes from API enums and `JsonStringEnumConverter` from `_diskJsonOptions` (no longer needed in API layer)
- Keep `RuleSetValidator.IsRegexType` fix to handle both string and number type values

## Capabilities

### New Capabilities
- `local-ruleset-writer`: Actor-based local ruleset persistence — accepts domain messages, converts to disk format with string enums, validates, and writes atomically

### Modified Capabilities
- `ruleset-api`: CRUD endpoints delegate to LocalRuleSetWriter actor instead of doing disk I/O directly
- `ruleset-management`: RuleSetMerger renamed to RuleSetReader; enum mapping extracted to shared utility

## Impact

- `FunkArr.RuleSet`: RuleSetMerger.cs renamed, new files added (RuleSetWriter, LocalRuleSetWriter, RuleSetEnumMapping)
- `FunkArr.Api`: RuleSetApiEndpoints.cs simplified — loses `_diskJsonOptions`, `SerializeForDisk`, direct `IDataFiles`/`IRuleSetValidator` deps
- `FunkArr.Api/Models/Enums.cs`: `JsonStringEnumMemberName` attributes removed
- `FunkArr.Messages`: New command/response records for local ruleset operations
- `FunkArr/Configuration`: LocalRuleSetWriter actor registered in AkkaSetupContainer
- `FunkArr.RuleSet.Tests`: Tests updated for renamed classes and new actor
- Architecture tests may need updates for the new actor naming
