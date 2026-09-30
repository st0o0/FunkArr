## Context

FunkArr's TV search pipeline: Mediathek query → scoring (regex S/E extraction) → enrichment (TVDB lookup) → Newznab response. Enrichment currently only runs for items where scoring produced no S/E (`Identity.Season is null && Identity.Episode is null` in `TryGetEnrichmentRequest`, line 216 of `TvSearchWorkerState.cs`). When the regex extracts S/E from the Mediathek title, those numbers are used as-is in the Newznab response — but they often don't match TVDB's numbering.

Examples:
- Das Traumschiff: Mediathek uses real season numbers (S31E03), TVDB uses flat S1 (S1E110)
- Hubert ohne Staller: Mediathek S08, TVDB S13
- Sturm der Liebe: Absolute episode E4636 but no season, Newznab season attribute empty

## Goals / Non-Goals

**Goals:**
- Enrichment runs for all matched items when enabled, overriding regex-extracted S/E with TVDB numbers
- Absolute episode numbers get a season attribute in search results
- No changes to the enrichment actor itself — it already works correctly

**Non-Goals:**
- Changing how scoring/regex extracts S/E from Mediathek titles (that stays as-is for scoring traces)
- Adding new enrichment methods (airdate/title matching already exist)
- Changing the enrichment config on individual rulesets (the code change enables enrichment to run; rulesets may still need enrichment enabled)

## Decisions

### 1. Always send matched items for enrichment when enrichment is enabled

**Decision**: Remove the `Identity.Season is null && Identity.Episode is null` filter from `TryGetEnrichmentRequest()`. Send all matched items with `HasScoringMetadata` for enrichment, regardless of whether scoring already extracted S/E.

**Rationale**: TVDB is the authoritative source for Sonarr. If enrichment resolves an episode, its S/E should always win over regex-extracted Mediathek numbers. The enrichment actor already handles the case where it can't resolve — it simply doesn't return that item, leaving the regex-extracted S/E intact as a fallback.

**Alternative considered**: Only run enrichment as a fallback when the regex S/E doesn't match any Sonarr episode — rejected because this would require knowledge of Sonarr's episode list at search time, adding coupling.

### 2. Pass regex-extracted S/E to enrichment as hints

**Decision**: Include the regex-extracted season and episode in the `EpisodeCandidate` sent to enrichment, so the enrichment actor can use them as additional matching signals.

**Rationale**: The existing `EpisodeCandidate` record has `Season` and `Episode` fields (currently always null since only items without S/E were sent). Passing the regex values lets the enrichment actor potentially use them to disambiguate when title/airdate matching is ambiguous.

### 3. Enrichment results always override regex S/E

**Decision**: In `Apply(EnrichEpisodesCompleted)`, always overwrite `Identity.Season` and `Identity.Episode` with the enrichment result, even when they were already set by regex.

**Rationale**: If enrichment found a match with sufficient confidence, it's the correct TVDB number. The regex number was Mediathek-native and is the wrong namespace for Sonarr.

### 4. Default season for absolute episode numbers

**Decision**: In `ReleaseVariant.BuildIdentifier()`, when only an episode number exists without a season, default to season `1` in the Newznab result.

**Rationale**: Sonarr expects both season and episode attributes. TVDB absolute-numbered shows typically map to season 1. Without a season attribute, Sonarr can't match the result.

## Risks / Trade-offs

- **[Risk] Enrichment volume increase**: Sending all matched items instead of only unidentified ones increases TVDB API calls. → **Mitigation**: The enrichment actor already caches TVDB episode data per series. The extra items are matched against the same cached episode list, so the TVDB API call count stays the same (one per unique tvdbId per search).
- **[Risk] Wrong enrichment override**: If enrichment matches incorrectly (low confidence), it could replace a correct regex S/E with a wrong one. → **Mitigation**: Enrichment only overrides when confidence meets the configured threshold. Low-confidence results are not applied.
- **[Risk] Regression for currently working shows**: Shows where regex S/E matches TVDB would now go through enrichment too. → **Mitigation**: If enrichment resolves to the same S/E, nothing changes. If it resolves differently, the TVDB number is correct by definition.
