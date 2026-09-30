## Context

Local rulesets are saved via `POST /api/rulesets` and `PUT /api/rulesets/{id}` with minimal validation (kebab-case ID + non-empty topic on create, nothing on update). The raw JSON body is written to disk and the merger silently ignores invalid structures. The `ruleset.schema.json` exists for CI validation but is not used at runtime. There is no export mechanism for contributing local rulesets back to the community.

`RuleSetMerger` already contains the full merge logic (`Resolve`/`Merge` methods) that produces a flattened result from community + local sources. The API already exposes `GET /api/rulesets/{id}/raw` for raw JSON access.

## Goals / Non-Goals

**Goals:**
- Validate all local ruleset saves against the JSON schema and business rules
- Return all errors at once with human-readable, contextual messages
- Export any ruleset with a local component as a standalone community-ready JSON
- Display validation errors in the builder UI

**Non-Goals:**
- Bulk export/import
- Direct GitHub PR creation
- Schema editing in UI
- Validating community rulesets at runtime (CI handles that)

## Decisions

### D1: JSON Schema validation library → JsonSchema.Net

**Choice**: `JsonSchema.Net` (json-everything) over `NJsonSchema`.

**Why**: JsonSchema.Net supports Draft 2020-12 (which the schema uses per the CI `--spec=draft2020` flag). NJsonSchema only supports Draft 4/6/7. JsonSchema.Net is also lighter, has no Newtonsoft dependency, and works with System.Text.Json natively.

**Alternatives considered**: NJsonSchema (wrong draft version), manual validation (too brittle), FluentValidation (not schema-based).

### D2: Schema embedding → Assembly embedded resource in FunkArr.RuleSet

**Choice**: Embed `ruleset.schema.json` as an embedded resource in `FunkArr.RuleSet.csproj`.

**Why**: Always available regardless of whether community rulesets have synced. The schema is the source of truth at `data/community/ruleset.schema.json` — the build copies it into the assembly. No filesystem dependency at runtime.

**How**: Add `<EmbeddedResource Include="..\..\data\community\ruleset.schema.json" Link="ruleset.schema.json" />` to the csproj. Load via `Assembly.GetManifestResourceStream()` at startup, parse once, cache as singleton.

### D3: Validator service → `RuleSetValidator` in FunkArr.RuleSet

**Choice**: Sealed class `RuleSetValidator` implementing `IRuleSetValidator`, registered as singleton.

**Why**: Lives in the domain project closest to ruleset logic. Used by FunkArr.Api for save validation and export validation. Interface in FunkArr.Core for DI.

```
FunkArr.Core:     IRuleSetValidator (interface)
FunkArr.RuleSet:  RuleSetValidator (implementation)
FunkArr.Api:      consumes IRuleSetValidator
```

### D4: Error message format → Contextual with rule reference

**Choice**: Map JSON path errors to human-readable messages referencing rule ID or index.

**Format**: `{ "field": "rules[1].filters.all[0].field", "message": "Filter field is required. Use one of: duration, title, topic, channel." }`

**Mapping approach**: After schema validation, walk the error list. Extract the rule index from the JSON path, look up the rule's `id` field if present, and compose the message. Business rule errors (confidence range, strategy enum, regex syntax) are appended to the same error list.

**Response format**: HTTP 422 with `{ "errors": [...] }` array. Each error has `field` (dotted path with rule context) and `message` (human-readable fix instruction).

### D5: Export flattening → Reuse RuleSetMerger output + serialize

**Choice**: For export, call `RuleSetMerger.Resolve()` to get the merged result, then serialize back to the canonical JSON structure. Strip `standalone` and `disable` fields. Schema-validate before returning.

**Why**: The merge logic already exists and is tested. Re-serializing ensures the export matches the schema exactly. Schema validation before export guarantees the output is valid.

**New code needed**: A `RuleSetExporter` that takes a ruleset ID, reads community + local JSON via `DataFiles`, calls the merger, serializes to the schema-conformant JSON structure, validates, and returns the result.

### D6: Export endpoint → `GET /api/rulesets/{id}/export`

**Choice**: Returns the flattened JSON with `Content-Disposition: attachment; filename="{id}.json"` and `Content-Type: application/json`.

**Why**: Download-as-file behavior makes it easy to save and submit. The filename matches the expected community contribution format.

## Risks / Trade-offs

- **[Schema drift]** The embedded schema could go stale if someone edits the source but doesn't rebuild. → Mitigation: CI validates community rulesets against the source schema; the embedded copy is always from the same source file via the build link.
- **[Validation strictness]** Strict validation might reject rulesets that previously saved fine. → Mitigation: Validation only applies to new saves, not to loading existing files. The merger remains lenient when reading.
- **[Export round-trip fidelity]** Serializing the merged result back to JSON might lose ordering or formatting. → Mitigation: Use `JsonSerializerOptions` with `WriteIndented = true` and consistent property ordering.
