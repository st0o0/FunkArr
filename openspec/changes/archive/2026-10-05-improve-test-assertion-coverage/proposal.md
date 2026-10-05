## Why

Several tests across the FunkArr test suite assert far less than the code path under test actually proves. Two concrete patterns were found during exploration: endpoint-registration tests that only check an endpoint exists but never exercise a request/response, and `Assert.NotNull(x)` immediately after `ExpectMsg<T>()` in Akka TestKit tests that adds no information beyond what `ExpectMsg<T>()` already proved. These tests pass without verifying the behavior they claim to cover, so regressions in response content or actor payloads can slip through. `test-assertion-hygiene` already governs null-guard and collection-indexing hygiene; it has no rule yet about assertion *depth* (vacuous asserts, registration-only endpoint tests).

## What Changes

- Document two new assertion-hygiene rules in `test-assertion-hygiene`:
  - An assertion immediately following `ExpectMsg<T>()` (or any point where the value's non-null type-correctness is already proven) must check actual content (property values, message text, etc.), not merely re-assert non-nullness.
  - A test for an API endpoint registration (`Map*Api`) must exercise the endpoint via a request and assert on the response (status code and/or body), not only assert that `IEndpointRouteBuilder.DataSources` is non-empty.
- Fix the two concretely identified cases:
  - `src/FunkArr.Api.Tests/RuleSetApiEndpointTests.cs`, `RuleSetWriteApiEndpointTests.cs`, `RuleSetTestApiEndpointTests.cs`: replace the registration-only check with real request/response assertions per endpoint group (or consolidate if they test the same route).
  - `src/FunkArr.Enrichment.Tests/EnrichmentManagerTests.cs`: replace `Assert.NotNull(stats)` and `Assert.NotNull(response)` with content assertions (e.g. property values on `CacheStatsResult`, failure message on `EnrichEpisodesFailed`), matching the pattern already used in the file's third test.
- Audit every test project (`src/*.Tests`, including `FunkArr.IntegrationTests`) for the same two patterns and fix each instance found, following the same before/after shape as the two seed cases above.

## Capabilities

### New Capabilities
(none)

### Modified Capabilities
- `test-assertion-hygiene`: add requirements for assertion depth — no vacuous `Assert.NotNull` after an already-proven non-null/typed value, and endpoint-registration tests must verify actual request/response behavior.

## Impact

- Affected test projects: `FunkArr.Api.Tests`, `FunkArr.ArrApi.Tests`, `FunkArr.Download.Tests`, `FunkArr.Enrichment.Tests`, `FunkArr.History.Tests`, `FunkArr.RuleSet.Tests`, `FunkArr.Scoring.Tests`, `FunkArr.Search.Tests`, `FunkArr.Persistence.Tests`, `FunkArr.IntegrationTests` (audit scope; only files with the identified weak patterns are changed).
- No production code changes — test-only.
- `AGENTS.md` test assertion conventions section gets the two new rules alongside the existing ones.
