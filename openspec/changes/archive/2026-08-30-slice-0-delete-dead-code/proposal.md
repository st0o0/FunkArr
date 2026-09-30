## Why

The foundation re-architecture design (§11) identified ~350 lines of dead-but-tested code, an empty folder, and test files that verify nothing. This unreachable code survived a prior migration whose archived spec already required its removal. Deleting it now — before any structural changes — reduces the surface area for every subsequent slice and eliminates false signals from tests that assert only that a TestProbe echoes.

## What Changes

- **Delete** second matching implementation in `RuleSetMatchingEngine.cs` (lines 935-1077) and its tests (lines 531-571) — zero production callers
- **Delete** `QualityProbeService`, `Mp4AtomParser`, `HlsManifestParser` — ~350 lines, DI-registered, tested, unreachable since the quality-probing spec was archived
- **Delete** `Search/Matching/MatchContext.cs` — defined, never constructed
- **Delete** `FakeNzbBuilder.BuildFakeNzbUrl` — dead duplicate of `NewznabResultMapper.BuildFakeNzbUrl`
- **Delete** dead `byStrategy` computation in `RulesetController.cs` — computed, mislabeled, never returned
- **Delete** `Shared/` directory — contents move to proper locations in later slices; anything remaining is dead
- **Delete** `Muxing/` (empty folder) and `FunkArr.Tests/Muxing/` (tests for code that moved)
- **Delete** `FunkArr.Tests/Api/RulesetControllerTests.cs` and `MatchIntelligenceControllerTests.cs` — they assert that a TestProbe echoes, no controller is instantiated
- **Remove** DI registrations and test references for all deleted types

**Not in scope:** `RuleSetGenerationService`, `SetupValidationService`, `ApiKeyValidationService` — these need replacement implementations before deletion (later slices).

## Capabilities

### New Capabilities

_None — this is pure subtraction._

### Modified Capabilities

- `ruleset-matching-engine`: Remove unreachable second matching implementation
- `quality-probing`: Delete the entire capability (already archived, code was never removed)
- `content-filter`: Remove dead `MatchContext` type
- `contract-tests`: Remove fake controller tests that verify nothing

## Impact

- **Code:** ~500+ lines of dead code removed across `FunkArr/` and `FunkArr.Tests/`
- **DI registrations:** `QualityProbeService`, `Mp4AtomParser`, `HlsManifestParser` removed from setup
- **Tests:** ~100 lines of misleading test code removed; remaining tests must stay green
- **Build:** Must compile and pass all remaining tests after every deletion
