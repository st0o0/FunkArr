## Why

FunkArr.Api has grown organically and lacks consistent conventions. Endpoints mix inline lambdas with extracted static helper methods, request/response models use strings where enums belong, route paths are inconsistent, and all models sit in a flat namespace. This makes the code harder to read, harder to maintain, and impossible to derive clear conventions for AGENTS.md.

## What Changes

### Inline lambdas only
- All endpoint handlers become inline lambdas. No `private static` handler methods in endpoint files.
- Extracted methods (`HandleCreate`, `HandleUpdate`, `HandleDelete`, `HandleExport`, `HandleGetRaw`, `EvictRuleSetCache`, `ExportResult`, `ResolveSelfUrl`, `ToVariantsResponse`, `ToApi(VariantPolicy)`) are inlined into their lambdas.
- `ParseMediaType` (×3 variants across endpoint files) disappears when query params become enum-typed.
- `RunTestEnrichment`/`EnrichEpisodes`/`EnrichMovies` (~170 lines) moves from the endpoint file into the domain layer as a new command to ScoringManager or EnrichmentManager. The `/test` endpoint stays a simple Ask→map→Result lambda.
- `ToApi`/`ParseMediaType` in `FeedApiEndpoints` move to `FeedMappingExtensions`.
- Exception: `SystemApiEndpoints` setup checks are co-located with the endpoint (self-contained health probes, no domain actor).

### Enums over strings
- `TestScoreRequest.MediaType`: `string?` → `MediaType?`
- `MediaInput.Type`: `string` → `MediaType`
- `DownloadHistoryRequest.Category`: `string?` → `MediaType?`
- Feed GET query param `type`: `string?` → `MediaType?`
- `FilterGroupTrace.Operator`: `string` → new `FilterOperator` enum (`All`, `Any`, `Not`)
- `FilterNodeTrace.Field`: `string?` → `FilterField?`
- `FilterNodeTrace.Op`: `string?` → `FilterOp?`
- `IdentificationTrace.Strategy`: `string?` → `IdentificationStrategy?`
- `CacheStatsResponse.OldestEntry`: `string?` → `DateTimeOffset?`
- `DownloadTimeSlotResponse.Start`/`End`: `string` → `TimeOnly`

### **BREAKING** Consistent route paths
- `POST /api/downloads/{id}/retry` → `POST /api/downloads/queue/{id}/retry`
- `POST /api/downloads/pause` → `POST /api/downloads/queue/pause`
- `POST /api/downloads/resume` → `POST /api/downloads/queue/resume`
- Feed `MapGet("")` → `MapGet("/")`

### Model organization
- Move models from flat `Models/` into `Models/{Feature}/` subfolders: Downloads, RuleSets, Scoring, Feed, Mediathek, System, Setup.
- Shared types (`MediaType`, `OperationResult`, `ErrorResponse`) into `Models/Shared/`.
- Split `Enums.cs` — each enum lives with the feature that owns it.
- Files contain either request records or response records, not both (unless matched pair for a single endpoint).

### AGENTS.md API conventions section
Add a new section documenting: inline-only lambdas, endpoint lambda shape, route path rules, model organization, enums-over-strings, and mapping extension placement.

## Capabilities

### New Capabilities
- `api-inline-handlers`: All endpoint handlers are inline lambdas with no extracted static handler methods. Test enrichment logic moves to domain layer.
- `api-route-consistency`: Route paths follow consistent nesting (queue ops under `/queue/`, etc).
- `api-model-organization`: Models organized in `Models/{Feature}/` subfolders with clean separation of request/response records and feature-scoped enums.

### Modified Capabilities
- `api-enum-types`: String properties in request/response models replaced with enum types; new `FilterOperator` enum added; `DateTimeOffset`/`TimeOnly` used instead of formatted strings.
- `api-request-models`: Query parameters that represent enums bind directly as enum types instead of strings.
- `api-response-models`: Response records use enum property types instead of `.ToString()` output.
- `api-mapping-extensions`: Mapping extensions become the only non-inline code the endpoint layer calls. Feed/System mappings move into proper extension files.

## Impact

- **Code**: FunkArr.Api (all 6 endpoint files, all model files, mapping extensions), FunkArr.Api.Tests (route changes affect test URLs), FunkArr.Scoring or FunkArr.Enrichment (new command for test-scoring-with-enrichment).
- **Frontend**: Route path changes for retry/pause/resume endpoints. Enum casing changes for new typed enum fields (FilterGroupTrace, FilterNodeTrace, IdentificationTrace). `CacheStatsResponse.OldestEntry` changes from ISO string to native DateTimeOffset. `DownloadTimeSlotResponse.Start`/`End` change from string to TimeOnly.
- **Docs**: AGENTS.md gets API conventions section.
- **Depends on**: `enum-conventions-warnings-as-errors` should land first (establishes exhaustive switch mapping pattern and warnings-as-errors that this change relies on).
