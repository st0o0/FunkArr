## Context

The FunkArr internal API uses Minimal APIs with Scalar for OpenAPI documentation. Existing patterns show `.WithSummary()` on every endpoint and `.WithDescription()` on roughly a quarter. The test suite uses xUnit v3 on Microsoft.Testing.Platform with Akka TestKit for actor tests, TestProbe registration for actor resolution, and direct unit tests for pure functions.

## Goals / Non-Goals

**Goals:**
- Every visible internal API endpoint has a `.WithDescription()` explaining what it does and its parameters
- Every endpoint declares its full response type surface (Produces + ProducesProblem for all returned status codes)
- HistoryWorker persistent actor has tests covering command handling, persistence, and recovery
- SabnzbdDownloadService and SabnzbdQueueService have tests covering all public methods including timeout/error paths
- ArrSetupClient has tests covering response parsing for success, validation errors, and connection failures
- All 5 mapping extension classes have test coverage

**Non-Goals:**
- ArrApi controller OpenAPI attributes (protocol adapters for Sonarr/Radarr, low priority)
- Download/Enrichment actor plumbing tests (state tests exist)
- Full integration tests for setup endpoints (would require real Arr instances)
- Changing endpoint behavior (metadata-only for the OpenAPI part)

## Decisions

### D1: Description style

Follow the pattern established by the RuleSet GET endpoints and MediathekSearchEndpoint -- one sentence explaining the endpoint's purpose, optionally followed by parameter notes. Keep descriptions under 2 sentences.

### D2: Test patterns for SABnzbd services

SabnzbdDownloadService and SabnzbdQueueService use `IActorRegistry.GetAsync<T>()` to resolve actors, then `Ask<T>()` with timeout. Tests will use the existing TestKit pattern from FunkArr.ArrApi.Tests: register TestProbes via the test server's actor registry, then verify messages and send responses.

### D3: ArrSetupClient test approach

ArrSetupClient wraps HttpClient. Tests will use a custom `HttpMessageHandler` stub that returns predefined responses (success JSON, validation error array, general error object, timeout). This avoids needing a real HTTP server while testing the JSON parsing logic.

### D4: HistoryWorker test approach

HistoryWorker is a persistent actor with sharding. Tests will use the TestKit pattern from FunkArr.History.Tests with in-memory persistence (already configured in test infrastructure). Test recovery by restarting the actor and verifying state restoration from persisted events.

### D5: Mapping extension tests

Pure function tests -- create domain objects, call the mapping extension method, assert the API model fields. No actor infrastructure needed. These go in FunkArr.Api.Tests.

## Risks / Trade-offs

- **[Description text may drift from implementation]** -- Accepted. Descriptions are documentation, not contracts. They improve developer experience in Scalar without adding coupling.
- **[SABnzbd service tests depend on actor message shapes]** -- Accepted. These tests validate the service layer's Ask/response handling, which is exactly what's missing.
