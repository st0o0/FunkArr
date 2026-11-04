## Context

FunkArr.Api is the internal REST API serving the Vue.js frontend. It currently has 6 endpoint files, ~34 model files in a flat `Models/` namespace, and 4 mapping extension files. The codebase has grown without enforced conventions, resulting in inconsistent patterns that make it hard to read and maintain.

Key issues:
- Endpoint files mix inline lambdas with extracted `private static` handler methods (no consistent rule for when to extract)
- 10 properties across request/response models use `string` where an enum or typed value belongs
- 3 different `ParseMediaType` methods with different accepted inputs
- Route paths inconsistently nest queue operations
- All models in a flat namespace with a single `Enums.cs` containing 12 unrelated enums
- ~170 lines of enrichment orchestration logic lives in `RuleSetApiEndpoints` instead of the domain layer

This change depends on `enum-conventions-warnings-as-errors` landing first, which establishes exhaustive switch expressions for enum mapping and warnings-as-errors.

## Goals / Non-Goals

**Goals:**
- Establish and enforce a single convention for endpoint handler style (inline lambdas only)
- Replace all string-typed enum properties with actual enum types
- Fix route path inconsistencies
- Organize models into feature-scoped subfolders
- Document all conventions in AGENTS.md so agents and contributors follow them
- Move test enrichment orchestration out of the API layer into the domain

**Non-Goals:**
- Refactoring the ArrApi (Newznab/SABnzbd adapter) — that's a separate project with different patterns (controller-based)
- Changing `FeedCrawlStatusOutput.LastOutcome` from string — it's a composite value with dynamic failure reasons, not a clean enum candidate
- Adding new API functionality — this is purely a structural cleanup
- Changing `MediathekSearchRequest.SortBy`/`SortOrder` — these pass through to the external MediathekViewWeb API
- Performance optimization — the changes are structural, not behavioral

## Decisions

### 1. Inline lambdas only, no extracted handler methods

All endpoint handlers will be inline lambdas in the `Map*` call chain. No `private static` methods that serve as handlers.

**Why over extracted methods:** Extracted methods create a split-attention problem — you read the route registration, then jump elsewhere to see what it does. With inline lambdas, the route path, the handler, and the OpenAPI metadata are all in one place. The rule is absolute: if a lambda is getting too long, that's a signal the logic belongs in the domain layer, not that we need a helper method.

**Alternatives considered:**
- Mixed approach (extract when > N lines): Rejected. Any threshold is arbitrary and leads to inconsistency.
- All extracted (method group references): Rejected. Loses the visual connection between route and handler.

**Exception:** `SystemApiEndpoints` setup checks (`CheckApiKey`, `CheckMediathekViewWeb`, etc.) stay co-located. These are self-contained health probes that don't map to domain actors. The checks themselves are inline in the setup endpoint's lambda body, but small utility methods within `SystemApiEndpoints` are acceptable since they're not endpoint handlers — they're probe logic.

### 2. Move test enrichment to domain layer

The `/api/rulesets/test` endpoint currently orchestrates scoring + enrichment by calling `ScoringManager` then `EnrichmentManager` in sequence (~170 lines). This violates the "endpoints are thin" principle.

**Approach:** Add a `TestScoreWithEnrichment` command to `ScoringManager` (or a new `TestScoringActor`). The actor handles the multi-step orchestration internally. The endpoint becomes a single Ask.

**Why ScoringManager:** The test scoring flow is already initiated through `ScoringManager`. Adding enrichment as an optional step keeps the entry point the same. The manager can forward to `EnrichmentManager` internally.

**Alternatives considered:**
- New dedicated actor: More isolation but adds a new actor for a single use case. Overkill.
- Keep in API with a service class: Violates the "no business logic in API layer" principle.

### 3. Enum-typed query parameters

ASP.NET Minimal APIs bind enum query parameters automatically — `?category=show` binds to `MediaType.Show` with case-insensitive matching. This eliminates all `ParseMediaType` methods.

**Breaking change for callers:** The accepted string values become the enum member names (case-insensitive). Current `"tv"` accepted by `DownloadsApiEndpoints.ParseMediaType` will no longer work — only `"show"` and `"movie"`. The frontend already uses `"show"`/`"movie"` for feed and ruleset requests, so this primarily affects the downloads history endpoint.

### 4. Feature subfolders for models

Models move into `Models/{Feature}/` subfolders. Namespace changes from `FunkArr.Api.Models` to `FunkArr.Api.Models.{Feature}`.

**Folder structure:**
```
Models/
├── Downloads/     (QueueResponse, HistoryResponse, HistoryRequest, Settings, MoveRequest, etc.)
├── RuleSets/      (ListResponse, DetailResponse, CreateRequest, UpdateRequest, RuleSetBody, etc.)
├── Scoring/       (TestScoreRequest, TestScoreResponse, ScoringDetail, ScoringHistory, etc.)
├── Feed/          (FeedOverviewResponse)
├── Mediathek/     (SearchRequest, SearchResponse)
├── System/        (SetupHealthCheck, VersionResponse, StorageStatus, CacheStats, Routes, Variants)
├── Setup/         (CreateArrResourceRequest, ArrPayloads)
└── Shared/        (MediaType, OperationResult, ErrorResponse, ValidationErrorResponse)
```

**Why subfolders over flat with naming convention:** Subfolders map directly to endpoint files, making navigation obvious. The namespace also makes `using` declarations self-documenting.

**`Enums.cs` split:** Each enum moves to the feature folder that owns it. `FilterField`, `FilterOp`, `FilterOperator`, `IdentificationStrategy`, `TitlePartType` → `RuleSets/`. `QueueStatus`, `QueuePhase`, `QueuePriority`, `HistoryStatus` → `Downloads/`. `MatchMethod`, `RuleOutcome` → `Scoring/`. `SearchSource` → `Scoring/` (used in scoring history). `MediaType`, `MediathekVariant`, `VariantPolicy` → `Shared/` (cross-feature). `EnrichmentMethod`, `RuntimeMode` → `RuleSets/` (enrichment config is part of ruleset definition). `CheckStatus` stays in `System/`.

### 5. Route path fixes

Three routes move under `/queue/`:
- `POST /{id}/retry` → `POST /queue/{id}/retry`
- `POST /pause` → `POST /queue/pause`
- `POST /resume` → `POST /queue/resume`

**Rationale:** All other queue-item operations (`force-start`, `move`, `priority`, `swap`) already live under `/queue/`. `retry`, `pause`, and `resume` are queue operations too. Feed root changes from `""` to `"/"` for consistency.

### 6. Mapping extensions as the only non-inline API code

Mapping extensions (`ToApi()`, `ToMessage()`) in `Extensions/` are the only code that endpoint lambdas call besides `IActorRegistry.GetAsync` and `actor.Ask`. This is already the pattern — the decision is to formalize it and move stray mappings (Feed `ToApi`, System `ToVariantsResponse`) into proper extension files.

## Risks / Trade-offs

**[Frontend route changes]** → Three endpoint paths change. Coordinate with frontend to update API calls for retry, pause, resume. Low risk: the frontend is in the same repo.

**[Namespace changes from model reorg]** → Every file that uses `FunkArr.Api.Models` needs updated `using` statements. Large mechanical diff but zero logic change. → Run in a single task, verify with `dotnet build`.

**[Enum query param binding strictness]** → ASP.NET returns 400 for invalid enum values instead of silently passing `null`. This is actually better behavior but may surface previously-ignored bad requests from the frontend. → Verify frontend sends valid enum values for all affected parameters.

**[Test enrichment refactor touches domain layer]** → Adds a new message type and handler to ScoringManager. Low risk since it's additive (new command, doesn't change existing commands). → Test with existing test scoring scenarios.

## Open Questions

- **SourceType enum location:** `SourceType` (Community/Local/Merged/Unknown) is currently mapped from a string in `RuleSetMappingExtensions`. It's used only in ruleset list responses. Should it stay in `RuleSets/` or `Shared/`? Leaning `RuleSets/` since it's domain-specific.
- **Enrichment models:** `EnrichmentConfigInput`/`EnrichmentConfigOutput` and related types are used by both RuleSet detail and test scoring. Keep in `RuleSets/` (since enrichment config is part of ruleset definition) or create `Models/Enrichment/`? Leaning `RuleSets/` to avoid a folder with 3 files.
