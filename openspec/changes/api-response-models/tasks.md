## 1. API response models

- [x] 1.1 Create `Models/RuleSetListEntry.cs` — sealed record mirroring `RegisteredRuleSetEntry` fields (ruleSetId, topic, aliases, tvdbId, imdbId, tmdbId)
- [x] 1.2 Create `Models/RuleSetDetail.cs` — sealed record mirroring `RuleSetDetailResult` with nested `RuleSetIdentity`, `RuleSetSource`, and `RuleSetDetailRule`
- [x] 1.3 Create `Models/ScoringHistory.cs` — sealed record mirroring `ScoringHistoryResult` with `ScoringSnapshotSummary`
- [x] 1.4 Create `Models/ScoringDetail.cs` — sealed record mirroring `ScoringDetailResult` with `ItemTrace`, `RuleTrace`, `FilterGroupTrace`, `FilterNodeTrace`, `IdentificationTrace`, `TracedIdentification`

## 2. Endpoint mapping

- [x] 2.1 Update `GET /api/rulesets` to map `RegisteredRuleSetsResult.Entries` → `RuleSetListEntry[]`
- [x] 2.2 Update `GET /api/rulesets/{id}` to map `RuleSetDetailResult` → `RuleSetDetail`
- [x] 2.3 Update `GET /api/rulesets/{id}/history` to map `ScoringHistoryResult` → `ScoringHistory`
- [x] 2.4 Update `GET /api/rulesets/{id}/history/{requestId}` to map `ScoringDetailResult` → `ScoringDetail`

## 3. Verification

- [x] 3.1 Build solution, run `dotnet format`, run all tests
