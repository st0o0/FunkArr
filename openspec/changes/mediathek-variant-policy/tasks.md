# Tasks

## 1. Variant model and policy

- [ ] 1.1 Add `MediathekVariant` enum (`Standard`, `AudioDescription`, `SignLanguage`, `PlainLanguage`, `OriginalVersion`) to `FunkArr.Messages` and `PersistedMediathekVariant` with the same member order to `FunkArr.Persistence`. Verify with `dotnet build`.
- [ ] 1.2 Add `VariantPolicy` enum and `VariantOptions` (`FunkArr:Variants`, one property per non-standard variant, default `Exclude`, `IsExcluded(MediathekVariant)`) to `FunkArr.Core`; register with `Bind` + `ValidateOnStart` in `ScoringSetupContainer`. Verify with unit tests for the defaults, a single `Allow`, and `Standard` never excluded.

## 2. Detection and release titles in Search

- [ ] 2.1 Add `VariantDetector` in `FunkArr.Search` (title + topic, word boundaries, case-insensitive, precedence AD > DGS > plain > OV) and carry the result as `SourceInfo.Variant`. Verify with a `Theory` covering every marker from the spec, topic-only markers, description ignored, "ADAC"/"OVB" false positives and the precedence case.
- [ ] 2.2 Extend `SceneRelease.FormatTitle` with variant tags (`AD`, `DGS`, `EinfacheSprache`; `OV` replacing `GERMAN`). Verify with tests for each spec scenario, and the existing title tests unchanged.

## 3. Policy in the search workers

- [ ] 3.1 Pass `VariantOptions` into `TvSearchWorkerState` / `MovieSearchWorkerState` (optional, default policy) and filter excluded items in `ToSearchCompleted` for scored, unmatched and unscored results. Verify with state tests for each path.
- [ ] 3.2 Set `ScoreCandidate.ExcludedVariant` (new optional field) for excluded sources in `TryGetScoringRequest` of both states. Verify with state tests.
- [ ] 3.3 Inject `IOptionsMonitor<VariantOptions>?` (optional) into `TvSearchWorker` / `MovieSearchWorker` and hand `CurrentValue` to the state. Verify with `dotnet build` and the existing worker tests passing.

## 4. Scoring trace and persistence

- [ ] 4.1 Add `ItemTrace.ExcludedVariant`; make `ScoringEngine.Score` skip rule evaluation for candidates with `ExcludedVariant` and record Matched=false, Score=0, no rule traces, the excluded variant. Verify with a scoring engine test.
- [ ] 4.2 Add `PersistedItemTrace.ExcludedVariant` and the int-cast mapping in History `PersistenceMapping`. Verify with round-trip tests, including a trace without exclusion mapping to `null`.

## 5. API, UI and docs

- [ ] 5.1 Add `ExcludedVariant` to the API `ItemTrace` model and mapping, and `GET /api/system/variants` returning the effective policy. Verify with a mapping test and an endpoint test asserting the default response.
- [ ] 5.2 Show the policy read-only on the Settings page and the exclusion reason in the scoring detail view; add i18n keys to en and de (de-AT and de-CH fall back to de). Verify the frontend CI check passes.
- [ ] 5.3 Document `FunkArr__Variants__*` in `docs/en/configuration.md` and `docs/configuration.md`, and the variant handling in `how-it-works` (both languages). Verify both languages describe the same defaults.
- [ ] 5.4 Run `dotnet format --verify-no-changes` and all affected test projects via CI and verify they pass.

## Workflow follow-up

- Open the PR stacked on `fix/rss-sync-empty-query` (#206), referencing #203.
- Archive the change after merge.
