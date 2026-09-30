## 1. Project setup

- [x] 1.1 Create `FunkArr.IntegrationTests` project with csproj (xunit.v3.mtp-v2, Microsoft.AspNetCore.Mvc.Testing, references to Api, ArrApi, RuleSet, Tests.Shared)
- [x] 1.2 Add project to `FunkArr.slnx`
- [x] 1.3 Create `FunkArrFixture.cs` (IAsyncLifetime, WebApplication + TestServer + TestProbes, HttpClient + GetProbe<T>())
- [x] 1.4 Create 7 collection definitions (Downloads, RuleSets, System, Setup, Mediathek, Newznab, Sabnzbd)
- [x] 1.5 Create `TestData.cs` with builder methods for QueueItem, HistoryItem, MediathekItem, RuleSetDetail, ScoringResult
- [x] 1.6 Verify project builds

## 2. Arr contract tests -- Newznab (P1)

- [x] 2.1 Create `Arr/NewznabCapsTests.cs` -- caps returns valid XML with categories
- [x] 2.2 Create `Arr/NewznabSearchTests.cs` -- tvsearch, movie search, general search with realistic probe responses
- [x] 2.3 Add NZB get test -- valid ID returns application/x-nzb content
- [x] 2.4 Add auth tests -- missing API key returns 401, wrong API key returns 401

## 3. Arr contract tests -- SABnzbd (P1)

- [x] 3.1 Create `Arr/SabnzbdVersionTests.cs` -- version, config, fullstatus with typed response verification
- [x] 3.2 Create `Arr/SabnzbdQueueTests.cs` -- queue with items, queue delete, priority, swap
- [x] 3.3 Create `Arr/SabnzbdHistoryTests.cs` -- history with items, history delete
- [x] 3.4 Create `Arr/SabnzbdDownloadTests.cs` -- addfile, retry, pause, resume
- [x] 3.5 Add error tests -- invalid mode, missing value, missing API key

## 4. Downloads contract tests (P2)

- [x] 4.1 Create `Api/DownloadQueueTests.cs` -- queue with active/queued items, empty queue
- [x] 4.2 Create `Api/DownloadHistoryTests.cs` -- history with entries, stats, categories
- [x] 4.3 Create `Api/DownloadMutationTests.cs` -- cancel (success + 404), retry (success + 400), force-start (success + 400)
- [x] 4.4 Create `Api/DownloadPipelineTests.cs` -- pause, resume
- [x] 4.5 Create `Api/DownloadReorderTests.cs` -- move (success + 400 + 404), priority (success + 400), swap (success + 400)
- [x] 4.6 Create `Api/DownloadSettingsTests.cs` -- settings returns typed response

## 5. RuleSet contract tests (P3)

- [x] 5.1 Create `Api/RuleSetListTests.cs` -- list with entries, empty list
- [x] 5.2 Create `Api/RuleSetDetailTests.cs` -- detail with rules and enrichment config, 404 for unknown
- [x] 5.3 Create `Api/RuleSetMutationTests.cs` -- create (201 + 409 + 422), update (200 + 404 + 422), delete (200 + 404)
- [x] 5.4 Create `Api/RuleSetExportTests.cs` -- raw JSON, export download, 404 for unknown
- [x] 5.5 Create `Api/RuleSetScoringTests.cs` -- test scoring with traces, scoring history, scoring detail (200 + 404)

## 6. System, Setup, Mediathek contract tests (P4)

- [x] 6.1 Create `Api/SystemTests.cs` -- version, storage, cache, logs, routes
- [x] 6.2 Create `Api/SetupHealthTests.cs` -- setup health check returns all check entries
- [x] 6.3 Create `Api/SetupProvisionTests.cs` -- all 5 setup endpoints return ArrResourceResponse
- [x] 6.4 Create `Api/MediathekSearchTests.cs` -- search with results, search validation error (400)

## 7. Migration cleanup

- [x] 7.1 Delete `FunkArr.Api.Tests/Integration/DownloadApiIntegrationTests.cs`
- [x] 7.2 Delete `FunkArr.Api.Tests/Integration/SystemApiIntegrationTests.cs`
- [x] 7.3 Delete `FunkArr.Api.Tests/Integration/FunkArrTestServer.cs` from Api.Tests (keep ArrApi.Tests copy if still needed by unit tests, or delete if not)
- [x] 7.4 Delete `FunkArr.ArrApi.Tests/Integration/NewznabIntegrationTests.cs`
- [x] 7.5 Delete `FunkArr.ArrApi.Tests/Integration/SabnzbdIntegrationTests.cs`
- [x] 7.6 Delete `FunkArr.ArrApi.Tests/Integration/FunkArrTestServer.cs` from ArrApi.Tests if no remaining references

## 8. Verify

- [x] 8.1 Run `dotnet build src/FunkArr.slnx`
- [x] 8.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 8.3 Run `dotnet run --project src/FunkArr.IntegrationTests/FunkArr.IntegrationTests.csproj` -- all new tests pass
- [x] 8.4 Run all other test projects -- verify no regressions from migration
