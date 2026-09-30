## Context

The API layer (`RuleSetApiEndpoints`) currently handles ruleset CRUD by directly serializing to a disk-specific JSON format (string enums), validating against a JSON schema, and writing files via `IDataFiles`. This requires `_diskJsonOptions` with `JsonStringEnumConverter` and `JsonStringEnumMemberName` attributes on API enums — mixing the API's number-enum contract with the disk's string-enum format in a single layer. The `RuleSetMerger` already owns the read direction (disk strings → domain enums) with its own `JsonSerializerOptions`; the write direction needs a symmetric counterpart.

## Goals / Non-Goals

**Goals:**
- Separate API layer (number enums) from disk layer (string enums) cleanly
- Create a `LocalRuleSetWriter` actor that owns all local ruleset write operations
- Extract bidirectional enum mapping to a shared `RuleSetEnumMapping` utility
- Rename `RuleSetMerger` → `RuleSetReader` for symmetry with the writer
- Remove all disk-format concerns (`_diskJsonOptions`, `SerializeForDisk`, `JsonStringEnumMemberName`) from the API layer

**Non-Goals:**
- Changing the disk JSON format or schema
- Making the API accept string enums (stays number-only)
- Persisting rulesets in SQLite/Akka.Persistence (disk files stay)
- Changing the FileSystemWatcher-based reload mechanism

## Decisions

### 1. Standalone `LocalRuleSetWriter` actor, not inside `RuleSetManager`

The `RuleSetManager` is a read-only coordinator: scan, watch, query, delegate to workers. Adding write operations would violate its single responsibility and grow the actor. A separate `LocalRuleSetWriter` keeps the manager unchanged and follows the existing pattern where domain actors have distinct roles.

The writer does NOT tell the manager to reload — the existing `FileSystemWatcher` detects the disk change and triggers the debounced reload. This avoids coupling the writer to the manager's internal state.

**Alternative considered:** Adding write methods to `RuleSetManager`. Rejected because it conflates read coordination with write operations, and the watcher-based reload already handles synchronization.

### 2. `RuleSetWriter` as a plain service class, actor wraps it

The actual conversion (Messages → disk JSON) and validation logic lives in a non-actor `RuleSetWriter` class. The `LocalRuleSetWriter` actor is thin: receive message → call writer → write file → respond. This makes the serialization/validation logic testable without Akka TestKit.

### 3. Bidirectional `RuleSetEnumMapping` shared by reader and writer

The `TryParseStrategy` switch in the current merger maps disk strings → enums. The reverse (enums → disk strings) is needed by the writer. Both directions live in `RuleSetEnumMapping` as static extension methods. The reader and writer both reference this class. Covers: `IdentificationStrategy`, `FilterField`, `FilterOp`, `TitlePartType`, `EnrichmentMethod`, `RuntimeMode`, `MediaType`.

**Alternative considered:** Using `JsonStringEnumConverter` with `JsonStringEnumMemberName` attributes. Rejected because the legacy disk names (`itemTitleIncludes`, `byAbsoluteEpisodeNumber`) don't follow the C# enum naming convention and the explicit mapping is more readable and maintainable.

### 4. Message types follow project conventions

Commands: `CreateLocalRuleSet`, `UpdateLocalRuleSet`, `DeleteLocalRuleSet` — VerbNoun pattern.
Responses: `abstract record CreateLocalRuleSetResponse` with `CreateLocalRuleSetCompleted` / `CreateLocalRuleSetFailed`. Failed variants carry typed reasons: `NotFound`, `AlreadyExists`, `ValidationFailed(errors)`.

### 5. API endpoints become Ask-only

`HandleCreate`, `HandleUpdate`, `HandleDelete` become simple Ask calls to the writer actor, mapping the typed response to HTTP status codes. `HandleExport` also delegates to the writer since export reads local files and validates — same layer concern.

### 6. `RuleSetValidator.IsRegexType` handles both string and number

The validator's regex check must handle both `"type": "regex"` (disk files) and `"type": 1` (if raw API JSON is ever validated). This is already implemented.

## Risks / Trade-offs

- **2-second reload delay**: After the writer writes a file, the `FileSystemWatcher` debounce window means the `RuleSetManager` takes up to 2 seconds to reflect the change. The API already returns success before the manager reloads, and the output cache is evicted immediately, so subsequent GET requests will re-query the manager (which may still serve stale data for up to 2 seconds). This is the existing behavior and acceptable for a local dev tool.

- **Writer is a singleton actor**: All CRUD operations serialize through one actor. This is fine for the expected load (manual UI edits) but would need sharding if concurrent writes were a concern. Current behavior is identical (API endpoint handlers are also effectively serial per request).
