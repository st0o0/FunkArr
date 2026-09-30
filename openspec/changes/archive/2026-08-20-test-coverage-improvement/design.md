## Context

The test suite has 41 test files but coverage is uneven. Three integration tests boot the full ASP.NET host to check HTTP responses — expensive, slow, and shallow. Meanwhile core components like `MediathekClient`, `TvdbClient`, `SearchChildHelpers`, `PathMappingHelper`, and `RuleSetFileWriter` have no tests despite containing real logic.

The existing `TmdbClientTests` and `DownloadServiceTests` establish a clear testing pattern: `FakeHttpMessageHandler` for HTTP clients, temp directories for file I/O, and `FakeFileService` for filesystem abstraction. All new tests follow these patterns.

## Goals / Non-Goals

**Goals:**
- Remove integration tests that provide false confidence (status-code-only checks)
- Add focused unit tests for the 5 highest-value untested files
- Rename misnamed test file to match its actual subject
- Follow existing test patterns (FakeHttpMessageHandler, FakeFileService) for consistency

**Non-Goals:**
- Refactoring `DownloadQueueActor` (separate change — too much logic in one actor)
- Testing `*Stages.cs` (thin Akka Streams glue wrapping already-tested services)
- Testing DTOs, enums, options records, diagnostics, or controllers
- Achieving arbitrary coverage percentage targets

## Decisions

### 1. Delete integration tests rather than keep alongside unit tests
Integration tests that only check "does the endpoint return 200" duplicate what a manual smoke test does. They add CI time (~3s cold-start per `WebApplicationFactory`) without catching real bugs. The controller logic is thin delegation — if the underlying services are tested, the endpoints work.

**Alternative considered:** Keep integration tests and add unit tests too. Rejected because the integration tests have never caught a bug that unit tests wouldn't, and they slow CI.

### 2. Use FakeHttpMessageHandler pattern for all HTTP client tests
`TmdbClientTests` already uses `FakeHttpMessageHandler` from `FunkArr.Tests.Shared`. `MediathekClient` and `TvdbClient` follow the same constructor-injected `HttpClient` pattern, so the same fake works directly. No need for a mocking framework.

### 3. Test SearchChildHelpers as two separate concerns
`BuildGenericPipelineRecord` is a pure function — test inputs → outputs directly. `SearchMediathekAsync` wraps `MediathekClient` with error handling — test it by injecting a `MediathekClient` backed by `FakeHttpMessageHandler` and verifying the fallback-to-empty-array behavior.

### 4. Test RuleSetFileWriter with real temp directory
`RuleSetFileWriter.Write` does `Directory.CreateDirectory` + `File.WriteAllText`. The logic to test is slug generation from topic + correct JSON serialization. Use a real temp directory (like `DownloadServiceTests` does), verify file exists and content deserializes back.

### 5. Rename DownloadQueueActorTests.cs without changing content
The file contains `DownloadEventsTests` class testing `DownloadEvents` and `DownloadJob` records. Just rename the file to match the class. No test logic changes.

## Risks / Trade-offs

**[Losing endpoint smoke coverage]** → The integration tests caught "does the app boot" regressions. Mitigation: the Docker build + `healthz` endpoint in CI already covers this. If desired, a single minimal boot-test could be added later.

**[MediathekClient metrics counters in tests]** → `MediathekClient` and `TvdbClient` use `FunkArrMetrics.Instance` for counters/histograms. These work fine in tests — `System.Diagnostics.Metrics` doesn't throw without a listener. No special setup needed.
