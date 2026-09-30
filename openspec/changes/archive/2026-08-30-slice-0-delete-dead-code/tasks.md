## 1. Quality probing dead code

- [x] 1.1 Delete `QualityProbeService`, `Mp4AtomParser`, `HlsManifestParser` source files and remove their DI registrations
- [x] 1.2 Delete quality probing test files and any associated verified snapshots
- [x] 1.3 Build and run tests to verify green

## 2. Matching engine dead code

- [x] 2.1 Delete the second matching implementation in `RuleSetMatchingEngine.cs` (lines 935-1077)
- [x] 2.2 Delete the corresponding tests in `RuleSetMatchingEngineTests.cs` (lines 531-571)
- [x] 2.3 Delete `Search/Matching/MatchContext.cs`
- [x] 2.4 Build and run tests to verify green

## 3. Dead duplicates and unused computations

- [x] 3.1 Delete `FakeNzbBuilder.BuildFakeNzbUrl` (dead duplicate of `NewznabResultMapper.BuildFakeNzbUrl`)
- [x] 3.2 Delete the dead `byStrategy` computation in `RulesetController.cs` (lines 285-289)
- [x] 3.3 Build and run tests to verify green

## 4. Fake controller tests

- [x] 4.1 Delete `FunkArr.Tests/Api/RulesetControllerTests.cs`
- [x] 4.2 Delete `FunkArr.Tests/Api/MatchIntelligenceControllerTests.cs`
- [x] 4.3 Build and run tests to verify green

## 5. Empty folders and junk drawer

- [x] 5.1 Delete `Muxing/` folder (empty) and `FunkArr.Tests/Muxing/` folder
- [x] 5.2 Assess `Shared/` — delete only files confirmed dead by grep; leave referenced files for later slices
- [x] 5.3 Build and run tests to verify green

## 6. Final verification

- [x] 6.1 Full build and test run — all tests pass, no warnings from deleted references
- [x] 6.2 Commit all deletions
