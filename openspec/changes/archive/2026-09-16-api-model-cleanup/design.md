## Context

FunkArr.Api has a `Models/` directory with typed API models and mapping extensions — a solid foundation. ArrApi already uses `[AsParameters]` request records (`IndexerRequest`, `DownloadGetRequest`, `DownloadPostRequest`). However, several FunkArr.Api endpoints still bind parameters inline in lambda signatures, and the RuleSetApiEndpoints list handler is a complex inline lambda while its siblings are cleanly extracted. Timeout values are scattered magic numbers.

## Goals / Non-Goals

**Goals:**
- All FunkArr.Api endpoints with 3+ query parameters use `[AsParameters]` request records
- RuleSetApiEndpoints list handler extracted to `HandleList` static method
- Duplicate `SerializeForDisk` consolidated
- Timeout values use named constants

**Non-Goals:**
- Changing ArrApi (already clean)
- Changing API behavior or response shapes
- Adding validation beyond what ASP.NET model binding provides
- Refactoring SSE streaming endpoints

## Decisions

### Decision 1: Request records in FunkArr.Api.Models

New request records go in `FunkArr.Api.Models/` alongside existing types:
- `MediathekSearchRequest`: q, channel, topic, durationMin, durationMax, offset, limit, sortBy, sortOrder
- `DownloadHistoryRequest`: start, limit, category

Each uses `[AsParameters]` with `[FromQuery]` on properties, matching the ArrApi pattern.

### Decision 2: Extract HandleList in RuleSetApiEndpoints

The list lambda (lines ~37-100) does fan-out to 3 actors + parallel stats queries + dictionary join. Extract to `private static async Task<IResult> HandleList(IActorRegistry registry)` — same pattern as HandleCreate/HandleUpdate/HandleDelete/HandleExport.

### Decision 3: Consolidate SerializeForDisk

Two identical `SerializeForDisk` methods at lines 279 and 291 in RuleSetApiEndpoints — consolidate into one method that takes the common interface or use a generic approach.

### Decision 4: Timeout constants as static fields

Keep timeout constants as `private static readonly TimeSpan` fields in each endpoint class (current pattern), but give them descriptive names and ensure consistency. No shared constants class needed — each endpoint class owns its timeouts.

## Risks / Trade-offs

- **[Minimal risk]** `[AsParameters]` is the standard ASP.NET pattern; OpenAPI generation improves automatically.
- **[SerializeForDisk consolidation]** Need to verify both methods truly have identical logic before merging.
