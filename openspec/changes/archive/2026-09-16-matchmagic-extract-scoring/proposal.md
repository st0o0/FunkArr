## Why

`MatchMagicActor.cs` is 576 lines, but ~550 of those are static pure functions — filter evaluation, identification strategies, title construction, regex capture, German date parsing, umlaut normalization. The actor shell is only ~25 lines of plumbing (constructor + `Handle` method that dispatches and tells history). Extracting the pure logic into a standalone `ScoringEngine` class makes it unit-testable without Akka TestKit and keeps the actor as thin routing plumbing per project convention.

## What Changes

- Extract all static methods from `MatchMagicActor` into a new `ScoringEngine` static class in `FunkArr.MatchMagic`
- `MatchMagicActor` becomes a thin receive actor: receive `ExecuteScoring`, call `ScoringEngine.Score(...)`, tell result to sender, tell history to shard region
- `ScoringEngine` contains: filter evaluation (traced), identification strategies (traced), title construction, regex capture, German date extraction, umlaut normalization, metadata building
- Add unit tests for `ScoringEngine` directly (no Akka TestKit needed)

## Capabilities

### New Capabilities
- `scoring-engine`: Pure scoring engine extracted from MatchMagicActor — filter evaluation, identification, title construction, and all supporting functions as a testable static class

### Modified Capabilities
- `matchmagic-evaluation`: MatchMagicActor delegates to ScoringEngine instead of containing the logic directly

## Impact

- **FunkArr.MatchMagic**: `MatchMagicActor.cs` shrinks from ~576 to ~40 lines, new `ScoringEngine.cs` with ~530 lines
- **FunkArr.MatchMagic.Tests**: New direct unit tests for ScoringEngine without Akka TestKit
- **No message changes, no API changes, no breaking changes** — purely internal refactor
