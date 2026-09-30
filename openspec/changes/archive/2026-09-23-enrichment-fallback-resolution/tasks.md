## 1. Enrichment candidate selection

- [x] 1.1 Update `TvSearchWorkerState.TryGetEnrichmentRequest()` to include all matched items with `HasScoringMetadata` — remove the `Identity.Season is null && Identity.Episode is null` filter
- [x] 1.2 Pass regex-extracted S/E as `ExistingSeason`/`ExistingEpisode` in each `EpisodeCandidate` (add these fields to the record if missing)
- [x] 1.3 Add a guard: skip enrichment if all matched items already have `Match` set (enrichment already ran)

## 2. Episode resolver — regex fallback

- [x] 2.1 Update the episode resolver (`EpisodeGuideActor` or resolution logic) to attempt configured methods first, then fall back to regex-extracted S/E if no TVDB match found
- [x] 2.2 When regex S/E exists and no configured method matches, return `EnrichedEpisode` with `Method=RegexExtracted`, `Confidence=1.0`, using the existing S/E values

## 3. Enrichment result override

- [x] 3.1 Update `TvSearchWorkerState.Apply(EnrichEpisodesCompleted)` to override `Identity.Season`/`Episode` even when they were already set by regex — enrichment results are authoritative

## 4. Absolute episode numbering

- [x] 4.1 In `ReleaseVariant.BuildIdentifier()` or `ToResultItem()`, when `Identity.Episode` is set but `Identity.Season` is null, default season to `"1"` in the `SearchResultItem`

## 5. Verification

- [x] 5.1 Build solution: `dotnet build src/FunkArr.slnx`
- [x] 5.2 Run format check: `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 5.3 Run all test projects
- [x] 5.4 Rebuild FunkArr container and verify Newznab search for a previously failing show (e.g. Das Traumschiff or Mord mit Aussicht) returns results matching Sonarr's episode numbers
