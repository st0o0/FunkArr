## Context

The RuleSet Builder UI serializes form state to JSON for save (PUT/POST), but omits `media.name` and `media.type` — both required by `ruleset.schema.json`. This makes every save attempt fail with 422. Separately, `RuleSetMerger.MergeMedia()` drops `Name` and `Type` during community/local merge, breaking export for any merged ruleset.

Analysis of community rulesets shows `media.name` differs from `topic` in ~28% of cases — especially movies (where `topic` is the broadcast slot like "Spielfilm" and `name` is the actual movie title) and shows with different TVDB names.

## Goals / Non-Goals

**Goals:**
- Builder produces schema-valid JSON on save (all required fields present, no null for integer fields)
- User can set media type (show/movie) and media name in the Builder UI
- Media name auto-fills from topic but can be independently edited
- Export works for merged rulesets (MergeMedia preserves Name/Type)
- Builder saves with `standalone: true` when editing community rulesets (UI saves complete state, merge is unnecessary)

**Non-Goals:**
- Changing the JSON Schema itself
- Adding media type/name fields to the ruleset list or detail views (they already display this data)
- Reworking the layering/merge model beyond the Name/Type bug fix

## Decisions

### Decision 1: Media name auto-sync with topic

The form state tracks `mediaName` and a `mediaNameManuallyEdited` flag. On mount, if the raw JSON's `media.name` differs from `topic`, set the flag. While the flag is unset, changing `topic` updates `mediaName` automatically. Once the user edits `mediaName` directly, the flag is set and sync stops.

This avoids forcing users to fill a redundant field for the common case (name = topic) while supporting the ~28% divergent case.

### Decision 2: Strip null optionals from serialized media

Instead of sending `{ tmdbId: null }` (which fails schema validation since schema says `"type": "integer"`), omit properties with null values. Build the media object conditionally:

```javascript
const media = { name: ..., type: ... }
if (form.tvdbId) media.tvdbId = form.tvdbId
if (form.imdbId) media.imdbId = form.imdbId
if (form.tmdbId) media.tmdbId = form.tmdbId
```

### Decision 3: Set standalone: true when Builder saves over community

When the Builder saves an edit of a community ruleset, the local file gets `standalone: true`. This is correct because the Builder always serializes the complete form state — all rules, full identity, full media. Merging with the community base would double-count rules.

For new rulesets (no community base), `standalone` is omitted (irrelevant).

### Decision 4: MergeMedia preserves Name and Type

Fix `MergeMedia()` to include `Name = local.Name ?? community.Name` and `Type = local.Type ?? community.Type`. This is defense-in-depth for hand-crafted local overlay files that use the merge path.

## Risks / Trade-offs

- **[Risk] Existing hand-crafted local files without media.name/type** → If someone hand-crafted a local overlay before this fix, MergeMedia now correctly falls through to community values. No behavior change for files that already had Name/Type.
- **[Risk] mediaName auto-sync could confuse users** → Mitigated by clearly labeling the field and showing it below topic. The auto-fill behavior is standard (like slug generation from title).
