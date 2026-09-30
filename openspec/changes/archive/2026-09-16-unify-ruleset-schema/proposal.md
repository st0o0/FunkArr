## Why

The RuleSet Detail API pre-formats rule data into display strings (strategy as `.ToString()`, filter summaries, match mode labels) while the Write API uses properly typed enums and structured data. This creates three inconsistent representations of the same data, prevents frontend i18n, and forces a separate `/raw` endpoint just so the Builder can get editable data. Unifying to one structured schema eliminates redundancy and makes the API self-consistent.

## What Changes

- **BREAKING**: Detail endpoint (`GET /api/rulesets/:id`) returns structured rule data instead of pre-formatted strings — `strategy` as JSON wire enum name, `filters` as structured tree, `titleRules` as typed array
- **BREAKING**: Remove `matchMode`, `filterSummary`, `titleParts` string fields from detail response; replace with `filters`, `titleRules` structured fields
- **BREAKING**: Remove `/api/rulesets/:id/raw` endpoint — Detail now serves the same purpose
- Remove backend formatting functions (`MatchModeFromStrategy`, `SummarizeFilters`, `FormatTitlePart`) from `RuleSetManagerState`
- Fix strategy serialization: use JSON wire name (`"itemTitleExact"`) not `.ToString()` (`"TitleExact"`)
- Frontend Detail-View renders structured data with i18n instead of displaying raw English strings
- Extract `strategyLabel()` into shared utility for reuse across Detail-View, Builder, Debugger, ScoringDetail
- Builder fetches from Detail endpoint instead of `/raw`

## Capabilities

### New Capabilities

_None — this is a consolidation of existing capabilities._

### Modified Capabilities

- `ruleset-api`: Detail response shape changes from pre-formatted strings to structured data; `/raw` endpoint removed
- `ruleset-ui`: Detail-View renders structured filters/titleRules/strategy instead of displaying pre-formatted strings
- `ruleset-builder-ui`: Builder uses Detail endpoint instead of `/raw`; shared strategy labeling

## Impact

- **Backend**: `FunkArr.Messages` (`RuleSetDetailRule`), `FunkArr.Api.Models` (`RuleSetDetailRule`), `FunkArr.Api.Extensions` (mapping), `FunkArr.RuleSet` (`RuleSetManagerState`), `FunkArr.Api` (endpoints)
- **Frontend**: `rulesets.ts` (types), `RuleSetDetail.vue`, `RuleSetBuilder.vue`, `DebuggerPanel.vue`, `LiveMatchPreview.vue`, i18n locales
- **Breaking API**: Detail response shape changes — no external consumers (internal UI only), version is 0.x
- **Tests**: `RuleSetMergerTests` may need assertion updates if they test detail rule formatting
