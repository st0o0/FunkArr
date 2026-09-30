## Why

The RuleSet list page shows 72+ rulesets as a flat list with no way to distinguish shows from movies. The `media.type` field exists in the community JSON files but is not exposed in the API or UI. As the catalog grows toward 200+ entries, users need type-based filtering to find what's supported. The schema also doesn't enforce `media` or `media.type`, allowing incomplete rulesets to slip through validation.

## What Changes

- **Schema enforcement**: make `media` required on the root ruleset object and `type` required within `mediaReference`. Fix the 2 existing community rulesets missing `media` (`fernsehfilme-und-serien-serien`, `unser-sandmaennchen`).
- **API enrichment**: add `mediaType` (`"show"` | `"movie"` | `null`) to the `RuleSetEntry` list response so the UI can filter without fetching each detail.
- **UI type filters**: add filter tabs (All / Shows / Movies) with counts and a type badge on each card in the RuleSet list page.
- **Community catalog**: generate `data/community/CATALOG.md` grouped by type with name, IMDB/TMDB links, and rule count. Link from root `README.md`.

## Capabilities

### New Capabilities
- `ruleset-catalog`: auto-generated catalog document listing all community rulesets grouped by media type

### Modified Capabilities
- `ruleset-list-enrichment`: add `mediaType` to `RegisteredRuleSetEntry` and `RuleSetSummaryEntry`
- `ruleset-ui`: add type filter tabs and type badge to the RuleSet list page

## Impact

- `data/community/ruleset.schema.json` — `media` and `media.type` become required
- `data/community/rulesets/` — 2 files fixed to include `media`
- `FunkArr.RuleSet` — `RegisteredRuleSetEntry` gains `MediaType` field
- `FunkArr.Api` — `RuleSetEntry` DTO gains `mediaType` projection
- `FunkArr.UI/src/views/RuleSetList.vue` — filter tabs and type badge
- `FunkArr.UI/src/api/rulesets.ts` — `RuleSetEntry` type updated
- `data/community/CATALOG.md` — new generated file
- `README.md` — link to catalog
