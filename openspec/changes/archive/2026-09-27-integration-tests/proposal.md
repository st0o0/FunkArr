## Why

Unit test coverage is solid (1018 tests) but integration tests are thin -- ~10 tests scattered across Api.Tests and ArrApi.Tests with no parallelism control. Tests use string checks instead of typed deserialization and respond with empty data. A regression in the Arr API would break Sonarr/Radarr integration silently. There is no single place that verifies "HTTP request in, correct HTTP response out" for all 56 user-facing flows.

## What Changes

- Create `FunkArr.IntegrationTests` project with collection-per-domain architecture
- 7 collections (Downloads, RuleSets, System, Setup, Mediathek, Newznab, Sabnzbd) each with own fixture instance
- `FunkArrFixture` evolved from existing `FunkArrTestServer` as `ICollectionFixture`
- `TestData` static class with builder methods for realistic domain objects
- Contract tests for all API endpoints: happy path with realistic data + error paths
- Typed response deserialization (never string checks)
- Migrate existing integration tests from Api.Tests and ArrApi.Tests
- Remove old integration test files after migration

## Capabilities

### New Capabilities

- `integration-test-infrastructure`: FunkArrFixture, collection definitions, TestData builders
- `integration-test-arr`: Newznab and SABnzbd contract tests (P1 -- external integration)
- `integration-test-downloads`: Download API contract tests (P2)
- `integration-test-rulesets`: RuleSet API contract tests (P3)
- `integration-test-system`: System, Setup, and Mediathek API contract tests (P4)

### Modified Capabilities

None -- this change only adds a test project and removes old test files.

## Impact

- **New**: `src/FunkArr.IntegrationTests/` project with ~60+ test files
- **Modified**: `src/FunkArr.slnx` to include new project
- **Removed**: Integration test files from `FunkArr.Api.Tests/Integration/` and `FunkArr.ArrApi.Tests/Integration/`
- **FunkArr.Tests.Shared**: May gain shared helpers used by both unit and integration tests
