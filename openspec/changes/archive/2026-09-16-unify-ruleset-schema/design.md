## Context

The RuleSet domain has a well-typed internal model (`MatchingRule` → `IdentificationSpec` → typed enums) and a well-typed Write API model (`RuleInput`). But the Detail-Read API (`RuleSetDetailRule`) converts everything to pre-formatted strings before sending to the frontend. This creates three inconsistent representations, prevents i18n, and forces a separate `/raw` endpoint for the Builder.

The frontend already has the logic to format strategy labels (`strategyLabel()` in the Builder) and render filter traces (`FilterGroupTraceView`). The Detail-View just doesn't use them because it receives pre-digested strings.

## Goals / Non-Goals

**Goals:**
- Single structured rule representation across Read and Write APIs
- Strategy serialized consistently as JSON wire name everywhere
- Frontend owns all display formatting and localization
- Remove `/raw` endpoint and backend formatting functions
- Shared strategy labeling across all frontend views

**Non-Goals:**
- Changing the internal domain model (`MatchingRule`, `IdentificationSpec`, etc.) — it's already correct
- Changing the JSON-on-disk format for ruleset files
- Refactoring the filter builder or title rules builder UI
- Adding new API capabilities

## Decisions

### 1. Reuse `RuleInput` shape for Detail response (not `MatchingRule`)

The Detail response needs a flat rule structure with `seasonRegex`, `episodeRegex`, `captureGroup`, `filters`, `titleRules` at the top level — matching what the frontend sends on Write and what JSON files contain on disk. This is the `RuleInput` shape.

The domain `MatchingRule` nests these into `IdentificationSpec` which is right for the scoring engine but wrong for the API contract. The API already flattens this for Write; it should flatten the same way for Read.

**Alternative considered**: Exposing `MatchingRule`/`IdentificationSpec` directly. Rejected because it would change the Write API contract too, and the nested structure (`identification.seasonPattern`) differs from the JSON-on-disk flat structure (`seasonRegex`).

### 2. New `RuleSetDetailRule` record with typed fields

Replace the string-based `RuleSetDetailRule` (in both Messages and Api.Models) with a structured record carrying:
- `Strategy` as `IdentificationStrategy` enum (serialized to wire name via `JsonStringEnumConverter`)
- `Filters` as `FilterGroupOutput` (nullable, structured `all`/`any`/`not` with typed conditions)
- `TitleRules` as `TitleRuleOutput[]` (nullable, typed parts)
- Keep `Id`, `Priority`, `Confidence`, `SeasonRegex`, `EpisodeRegex`, `CaptureGroup`

The Messages layer gets the structured record; the Api.Models layer gets a matching record. Mapping is a straightforward decomposition of `IdentificationSpec` back to flat fields.

### 3. Remove `matchMode` and `filterSummary` entirely

`matchMode` is derivable from `strategy` (only non-null for title strategies). `filterSummary` is a lossy text rendering. Both are replaced by the structured data — the frontend formats them.

### 4. Filter output model mirrors `FilterGroupInput`

The Write API already defines `FilterGroupInput` / `FilterNodeInput`. The Read response uses a parallel `FilterGroupOutput` / `FilterNodeOutput` structure with the same shape. This avoids making the input model do double duty (input models may gain validation attributes later).

### 5. Frontend: extract `strategyLabel()` to shared util

Move `strategyLabel()` from `RuleSetBuilder.vue` into a shared `composables/useStrategy.ts` (or `utils/strategy.ts`). All views import from there. The i18n keys stay in the `builder` namespace for now (they're not builder-specific, but renaming i18n keys is churn with no user impact — can be done later).

### 6. Frontend: adapt `FilterGroupTraceView` for non-trace use

`FilterGroupTraceView` currently renders filter trees with pass/fail coloring from trace data. Factor out a `FilterConditionList` that renders a filter tree without trace status, for use in the Detail-View. The trace view wraps this with its coloring. Alternatively, make the trace fields optional and conditionally render status.

### 7. Builder fetches from Detail endpoint

The Builder currently fetches from `GET /api/rulesets/:id/raw` to get the JSON-on-disk shape. With the unified schema, `GET /api/rulesets/:id` returns the same structured data the Builder needs. The `/raw` endpoint is removed. `getRuleSetRaw()` in `rulesets.ts` is replaced by `getRuleSetDetail()`.

The Builder's `onMounted` hydration code changes from mapping raw JSON fields to mapping the typed detail response — mostly the same field names, with `strategy` now consistent.

## Risks / Trade-offs

**[Risk] Detail response is larger than before** → Structured filters/titleRules are more verbose than summary strings. Mitigation: These are small objects (a rule rarely has more than 5 filter conditions); the size difference is negligible for an internal UI API.

**[Risk] Frontend needs new rendering code for Detail-View** → The Detail-View currently just renders strings. It now needs to format filters and title parts. Mitigation: Most of this logic exists in the Builder and Debugger already — it's extraction and reuse, not new invention.

**[Risk] `/raw` removal breaks Builder if done out of order** → Mitigation: Implement backend and frontend in the same change; the Builder and `/raw` removal happen together.
