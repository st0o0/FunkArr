## Context

The FunkArr internal API (`FunkArr.Api`) currently mixes three patterns for request/response handling:
1. **Typed models** — GET endpoints use well-structured records in `Models/` (good)
2. **Manual `JsonElement` parsing** — Create, update, and test endpoints accept raw JSON and walk properties by hand
3. **Anonymous types** — Mutation endpoints return `new { success, error }` objects with no schema

The API is OpenAPI-first (`api-openapi` spec), but these gaps mean several endpoints produce `Produces<object>()` or no schema at all. The Vue frontend works around this with untyped fetch responses.

## Goals / Non-Goals

**Goals:**
- All API endpoints use typed request and response models
- OpenAPI schema covers every endpoint (no `Produces<object>()`)
- Model files consolidated in `Models/` with consistent naming
- `RuleSetTestRequestParser` eliminated — STJ handles deserialization

**Non-Goals:**
- Changing any API contract (paths, field names, behavior) — wire format stays identical
- Adding validation attributes or FluentValidation — existing `IRuleSetValidator` stays
- Refactoring actor Ask patterns or error handling
- Changing the ArrApi (Newznab/SABnzbd) adapters

## Decisions

### Decision 1: STJ deserialization with `JsonConverter` for the test request's polymorphic types

The test request body contains `FilterSpec` with recursive `FilterNode` (either a condition or a nested group). STJ can handle the flat parts automatically but needs a custom `JsonConverter<FilterNode>` to distinguish condition vs group nodes. This replaces the manual parser with a single converter (~30 lines) instead of ~240 lines of hand-walking.

**Alternative**: Use `[JsonDerivedType]` with discriminator — rejected because the JSON contract has no discriminator field; the distinction is structural (presence of `all`/`any`/`not` keys).

**Alternative**: Keep `RuleSetTestRequestParser` for just the polymorphic part — rejected because the whole point is to let STJ handle the request; a partial solution leaves the parser alive for no real benefit.

### Decision 2: Request models live in `Models/` alongside response models

Request models (`CreateRuleSetRequest`, `UpdateRuleSetRequest`, `TestScoreRequest`) go in `Models/` with the response types. They're API-layer concerns, not domain messages.

**Alternative**: Separate `Requests/` folder — rejected; unnecessary indirection for a handful of records.

### Decision 3: Shared `OperationResult` for mutation responses

The `{ success: true/false, error: "..." }` pattern repeats across delete-queue, delete-history, and retry. A single `OperationResult(bool Success, string? Error)` record replaces all three anonymous usages.

### Decision 4: Create/Update keep `IRuleSetValidator` for body validation

The create and update endpoints currently accept `JsonElement`, strip `ruleSetId`, then validate the raw JSON string via `IRuleSetValidator` (JSON Schema-based). With typed models, the endpoint receives a deserialized object but still needs to pass the raw JSON to the validator. The handler will use `JsonSerializer.Serialize()` on the body (minus `ruleSetId` for create) before calling `validator.Validate()`.

**Alternative**: Move to model-level validation — rejected; the validator uses the community JSON Schema which must match the on-disk format exactly. Changing validation strategy is out of scope.

### Decision 5: `IdentificationSpec` strategy mapping stays in a helper

The test request JSON uses frontend strategy names (`"seasonAndEpisodeNumber"`, `"itemTitleExact"`) that map to domain enums + field combinations. This mapping logic moves from `RuleSetTestRequestParser` into a static helper method on the request model or a small mapper, keeping the endpoint handler clean.

## Risks / Trade-offs

- **[Risk] STJ deserialization silently drops unknown properties** → Acceptable; the current manual parser also ignores unknown fields. No behavior change.
- **[Risk] `FilterNode` JsonConverter adds a new maintenance surface** → Mitigated by keeping it small (~30 lines) and adding test coverage. The alternative (keeping the full parser) is worse.
- **[Risk] Serialize-then-validate round-trip for create/update** → Minor performance cost, but these are infrequent operations (user-initiated CRUD). Correctness > perf here.
