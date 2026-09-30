## Why

The test suite has three integration tests that boot the full ASP.NET host via `WebApplicationFactory` but only verify HTTP status codes and response shapes — no business logic. Meanwhile, several files with real logic have zero unit test coverage: two HTTP API clients (`MediathekClient`, `TvdbClient`), pipeline helpers (`SearchChildHelpers`, `PathMappingHelper`), and file I/O (`RuleSetFileWriter`). One test file (`DownloadQueueActorTests.cs`) is misnamed — it tests `DownloadEvents`, not the actor.

## What Changes

- **Remove** the 3 integration test files (`EndpointTests.cs`, `WebUiEndpointTests.cs`, `SetupValidationEndpointTests.cs`) — shallow smoke tests that don't catch real bugs
- **Add** unit tests for 5 source files with meaningful untested logic:
  - `MediathekClient` — HTTP client for Mediathek API (success/error/null paths)
  - `TvdbClient` — HTTP client for TVDB API (success/error/null paths)
  - `SearchChildHelpers` — query building and match record construction
  - `PathMappingHelper` — path parsing and mapping pure functions
  - `RuleSetFileWriter` — slug-based JSON file writing
- **Rename** `DownloadQueueActorTests.cs` to `DownloadEventsTests.cs` to match what it actually tests
- **Deliberately skip** `*Stages.cs` (thin Akka Streams glue over already-tested services), `DownloadQueueActor` (needs separate refactor), DTOs, enums, options, diagnostics, controllers

## Capabilities

### New Capabilities

_None — this change adds tests for existing capabilities, not new behavior._

### Modified Capabilities

_None — no spec-level requirements change._

## Impact

- `src/FunkArr.Tests/Integration/` — 3 files deleted
- `src/FunkArr.Tests/Search/` — 2 new test files (MediathekClientTests, SearchChildHelpersTests)
- `src/FunkArr.Tests/Api/` — 1 new test file (PathMappingHelperTests)
- `src/FunkArr.Tests/RuleSet/` — 1 new test file (RuleSetFileWriterTests)
- `src/FunkArr.Tests/DownloadClient/` — 1 file renamed, 1 new test file (TvdbClientTests goes in Search/)
- Net test count: remove ~20 integration tests, add ~25-30 focused unit tests
