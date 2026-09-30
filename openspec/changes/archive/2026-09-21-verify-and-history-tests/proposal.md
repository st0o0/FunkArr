## Why

FunkArr has zero Verify/JSON shape stability tests despite `Verify.XunitV3` being referenced in `Tests.Shared`. Persistence events stored in the Akka journal have no guard against accidental shape changes — a renamed property means data loss on recovery. The History domain has only 4 tests for an entire domain with persistent actors, state management, and stats aggregation.

## What Changes

- **New `FunkArr.Persistence.Tests` project**: Verify JSON shape tests for all 11 persistence events and 1 snapshot type, following the Njord pattern (Newtonsoft.Json serialization, `Verify()` shape test + manual roundtrip test per type, `.verified.txt` files in git)
- **Expand History tests**: ~54 new tests covering `HistoryState` (Apply, ComputeStats, Trim, QueryHistory, QueryDetail), `StatsCollectorState` (Apply, GetSnapshot, roundtrip), `HistoryWorker` (persist, query, recovery), and `StatsCollector` (backfill, query)
- **Test infrastructure**: `ModuleInitializer` with `DiffRunner.Disabled = true`, `.gitignore` entry for `*.received.*`, `.gitattributes` entry for `*.verified.txt`

## Capabilities

### New Capabilities

- `verify-json-tests`: Verify JSON shape stability tests for all persistence types using Newtonsoft.Json serialization and Verify.XunitV3
- `history-test-coverage`: Comprehensive test coverage for the History domain — state logic, actor behavior, persistence, and stats aggregation

### Modified Capabilities

_(none)_

## Impact

- **New project**: `FunkArr.Persistence.Tests` — references `FunkArr.Persistence`, `Verify.XunitV3`, `Newtonsoft.Json`
- **FunkArr.History.Tests**: Expanded from 1 test class (4 tests) to ~6 test classes (~58 tests)
- **FunkArr.Tests.Shared**: May need shared test data builders for complex nested types (ItemTrace tree)
- **Solution file**: Add `FunkArr.Persistence.Tests` project
- **Git config**: `*.received.*` in `.gitignore`, `*.verified.txt` in `.gitattributes`
