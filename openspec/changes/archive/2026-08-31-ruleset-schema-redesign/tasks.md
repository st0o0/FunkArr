## 1. Data Model Changes

- [x] 1.1 Add `MediaType` enum (`Show`, `Movie`) with `JsonStringEnumConverter` in `src/FunkArr.MatchMagic/MediaType.cs`
- [x] 1.2 Update `MediaRef` record: change `Type` from `string` to `MediaType` enum, default `MediaType.Show`
- [x] 1.3 Update `Rule` record: add `Id` as first required parameter (`string`)
- [x] 1.4 Update `RuleSet` record: remove `Source`, add `Standalone` (`bool`, default false) and `Disable` (`IReadOnlyList<string>?`, default null), make `Media` nullable, make `Confidence` nullable
- [x] 1.5 Update `RuleSet.FromJson`/`ToJson` serialization options if needed for new fields

## 2. RuleSet Resolver

- [x] 2.1 Create `RuleSetResolver` static class with `Resolve(RuleSet? community, RuleSet? local)` method in `src/FunkArr.MatchMagic/RuleSetResolver.cs`
- [x] 2.2 Implement merge algorithm: disable filtering, same-ID replacement, new-ID addition, priority sort
- [x] 2.3 Implement field merge: aliases union, confidence local-wins, media local-wins, topic community-canonical

## 3. JSON Schema

- [x] 3.1 Rewrite `data/community/ruleset.schema.json`: drop `source`, `$type`, `exactMatch`, `overrides`; add `id` to rule; add `standalone`/`disable`; change `media.type` to enum; two profiles via conditional validation

## 4. Test Resources

- [x] 4.1 Update `src/FunkArr.MatchMagic.Tests/Resources/tatort-ruleset.json`: add rule IDs, remove `source`

## 5. Tests

- [x] 5.1 Update `RuleSetDeserializationTests`: test new fields (id, standalone, disable, MediaType enum), test source field absence, test exactMatch rejection
- [x] 5.2 Update `RuleSetEvaluationTests`: add rule IDs to test data
- [x] 5.3 Add `RuleSetResolverTests`: community-only, local-only, both-null, extend with add/replace/disable, standalone, aliases union, confidence/media local-wins, topic canonical, disable unknown ID silent

## 6. Callers and Messages

- [x] 6.1 Update `LoadRuleSet` message and `MatchMagicManager` to work without `Source` on RuleSet
- [x] 6.2 Update any other callers that construct or reference `RuleSet.Source`

## 7. Validation

- [x] 7.1 Run `dotnet build FunkArr.slnx` and fix compilation errors
- [x] 7.2 Run `dotnet format` on changed files
- [x] 7.3 Run all MatchMagic tests: `dotnet run --project FunkArr.MatchMagic.Tests/FunkArr.MatchMagic.Tests.csproj`
