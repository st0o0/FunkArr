## 1. Extract ScoringEngine

- [x] 1.1 Create `ScoringEngine.cs` in `FunkArr.MatchMagic` as a `static class` — move all static methods from `MatchMagicActor` (filter evaluation, identification strategies, title construction, regex capture, German date extraction, umlaut normalization, metadata building, field resolution, numeric comparison)
- [x] 1.2 Create a public `Score` method on `ScoringEngine` that takes config + candidates and returns `(ScoredItem[] scored, ItemTrace[] traces)` — move the scoring loop from `MatchMagicActor.Handle`
- [x] 1.3 Move `_regexTimeout` and `_germanMonths` static fields to `ScoringEngine`

## 2. Slim down MatchMagicActor

- [x] 2.1 Rewrite `MatchMagicActor.Handle` to call `ScoringEngine.Score(...)` and use returned results for Sender.Tell and _historyRegion.Tell
- [x] 2.2 Remove all static methods from `MatchMagicActor` — only the constructor, Handle, and _historyRegion field should remain

## 3. Tests

- [x] 3.1 Add direct unit tests for `ScoringEngine.Score` — basic scoring with matching rules, no-match scenario, priority ordering
- [x] 3.2 Add unit tests for filter evaluation — All/Any/Not groups, short-circuit behavior, nested groups
- [x] 3.3 Add unit tests for identification strategies — SeasonAndEpisodeNumber, AbsoluteEpisodeNumber, TitleExact, TitleIncludes, AirdateExtraction
- [x] 3.4 Add unit test for regex timeout safety — verify pathological pattern returns gracefully

## 4. Verify

- [x] 4.1 Run `dotnet build src/FunkArr.slnx` and fix any compilation errors
- [x] 4.2 Run `dotnet format src/FunkArr.slnx` to apply code style
- [x] 4.3 Run existing MatchMagic test project to verify no regressions
- [x] 4.4 Run new ScoringEngine tests
