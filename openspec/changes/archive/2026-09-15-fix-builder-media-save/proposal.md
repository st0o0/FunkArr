## Why

The RuleSet Builder UI cannot save rulesets because it omits required `media.name` and `media.type` fields from the serialized JSON body. The schema requires both, so every save attempt returns 422 with validation errors. Additionally, `MergeMedia()` in the backend drops `Name` and `Type` during community/local merge, which breaks the export endpoint for any merged ruleset.

## What Changes

- Add **Media Type** radio/select (show/movie) to the Builder identity section
- Add **Media Name** text field to the Builder identity section, auto-filled from topic but independently editable
- Include `media.name` and `media.type` in the frontend's `serializeForm()` output
- Strip `null` values from optional media ID fields (`tmdbId`, `imdbId`) instead of sending `null` (schema expects integer/string, not null)
- Fix `MergeMedia()` in `RuleSetMerger` to preserve `Name` and `Type` during community/local merge
- Set `standalone: true` when the Builder saves over a community-only ruleset (the Builder always saves the complete state, so merge is unnecessary and can cause surprises)

## Capabilities

### New Capabilities

_none_

### Modified Capabilities

- `ruleset-builder-ui`: Add media type selector and media name field to the identity section; fix serialization to include all schema-required fields and strip null optionals
- `ruleset-layering`: Fix `MergeMedia()` to preserve `Name` and `Type` fields during merge

## Impact

- `FunkArr.UI/src/views/RuleSetBuilder.vue` — form state, template, and `serializeForm()`
- `FunkArr.RuleSet/RuleSetMerger.cs` — `MergeMedia()` method
- `FunkArr.RuleSet.Tests/` — merger tests for Name/Type preservation
- No API contract changes, no new endpoints
