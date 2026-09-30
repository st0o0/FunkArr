## Why

The Scalar API docs have summaries on all endpoints but 26 of 34 visible endpoints lack descriptions, 7 endpoints have no `Produces<T>()` metadata, and Downloads endpoints only declare 504 while also returning 400/404/500. On the test side, History has the thinnest coverage (persistent actor completely untested), ArrApi SABnzbd services have zero tests despite complex actor Ask patterns with timeout handling, and Api has the worst source-to-test ratio with mapping extensions and ArrSetupClient untested.

## What Changes

- Add `.WithDescription()` to all 26 internal API endpoints missing descriptions
- Add missing `.Produces<T>()` to 7 endpoints (system/setup, rulesets/raw, rulesets/export, all setup/* endpoints)
- Add missing `.ProducesProblem()` error codes to Downloads endpoints (400, 404, 500 where applicable)
- Write HistoryWorker actor tests (persistent actor lifecycle, Apply, GetSnapshot, recovery)
- Write History PersistenceMapping tests (Messages<->Persistence conversion)
- Write SabnzbdDownloadService tests (AddFile, DeleteFromQueue, DeleteFromHistory, Retry, SetPriority, Swap)
- Write SabnzbdQueueService tests (GetQueue, GetHistory, GetConfig, GetFullStatus, PauseQueue, ResumeQueue)
- Write ArrSetupClient tests (success/error response parsing, validation errors, connection failures)
- Write mapping extension tests (DownloadMapping, MediathekMapping, RuleSetMapping, ScoringMapping, TestScoreMapping)

## Capabilities

### New Capabilities

- `arr-setup-client-tests`: Test coverage for ArrSetupClient response parsing and error handling
- `history-actor-tests`: Test coverage for HistoryWorker persistent actor
- `sabnzbd-service-tests`: Test coverage for SabnzbdDownloadService and SabnzbdQueueService

### Modified Capabilities

- `api-openapi`: Add descriptions, Produces metadata, and error codes to all internal API endpoints
- `api-mapping-extensions`: Add test coverage for all 5 mapping extension classes

## Impact

- **FunkArr.Api**: OpenAPI metadata additions to all endpoint files (no logic changes)
- **FunkArr.Api.Tests**: New test files for ArrSetupClient and mapping extensions
- **FunkArr.ArrApi.Tests**: New test files for SabnzbdDownloadService and SabnzbdQueueService
- **FunkArr.History.Tests**: New test files for HistoryWorker and PersistenceMapping
