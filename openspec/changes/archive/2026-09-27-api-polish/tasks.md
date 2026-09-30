## 1. OpenAPI descriptions -- Downloads endpoints

- [x] 1.1 Add `.WithDescription()` to all 15 Downloads endpoints in `DownloadsApiEndpoints.cs`
- [x] 1.2 Add missing `.ProducesProblem()` for 400/404/500 status codes on Downloads endpoints that return those codes

## 2. OpenAPI descriptions -- System endpoints

- [x] 2.1 Add `.WithDescription()` to all 5 System endpoints in `SystemApiEndpoints.cs` (setup, version, storage, cache, routes)
- [x] 2.2 Add `.Produces<SetupHealthCheck>()` to the `/api/system/setup` endpoint

## 3. OpenAPI descriptions -- Setup and RuleSet endpoints

- [x] 3.1 Add `.WithDescription()` to all 5 Setup endpoints in `SetupArrEndpoints.cs`
- [x] 3.2 Add `.Produces<ArrResourceResponse>()` to all 5 Setup endpoints
- [x] 3.3 Add `.WithDescription()` to RuleSet mutation endpoints (POST, PUT, DELETE) in `RuleSetApiEndpoints.cs`
- [x] 3.4 Add appropriate produces metadata to `/api/rulesets/{id}/raw` and `/api/rulesets/{id}/export`

## 4. ArrSetupClient tests

- [x] 4.1 Create `ArrSetupClientTests.cs` in `FunkArr.Api.Tests` with stubbed HttpMessageHandler
- [x] 4.2 Test success response parsing (id, name, provider message extraction)
- [x] 4.3 Test validation error array parsing
- [x] 4.4 Test general error object parsing
- [x] 4.5 Test connection timeout and invalid URL handling

## 5. API mapping extension tests

- [x] 5.1 Create `DownloadMappingExtensionsTests.cs` in `FunkArr.Api.Tests`
- [x] 5.2 Create `MediathekMappingExtensionsTests.cs` in `FunkArr.Api.Tests`
- [x] 5.3 Create `RuleSetMappingExtensionsTests.cs` in `FunkArr.Api.Tests`
- [x] 5.4 Create `ScoringMappingExtensionsTests.cs` in `FunkArr.Api.Tests`
- [x] 5.5 Create `TestScoreMappingExtensionsTests.cs` in `FunkArr.Api.Tests`

## 6. History domain tests

- [x] 6.1 Create `HistoryWorkerTests.cs` in `FunkArr.History.Tests` with TestKit and in-memory persistence
- [x] 6.2 Test recording scoring results and querying history
- [x] 6.3 Test state recovery after actor restart
- [x] 6.4 Create `PersistenceMappingTests.cs` in `FunkArr.History.Tests` for Messages<->Persistence round-trip

## 7. SABnzbd service tests

- [x] 7.1 Create `SabnzbdDownloadServiceTests.cs` in `FunkArr.ArrApi.Tests` with TestProbe actors
- [x] 7.2 Test AddFile (success, missing file, actor timeout)
- [x] 7.3 Test DeleteFromQueue and DeleteFromHistory (success, invalid GUID, not found)
- [x] 7.4 Test Retry, SetPriority, Swap methods
- [x] 7.5 Create `SabnzbdQueueServiceTests.cs` in `FunkArr.ArrApi.Tests` with TestProbe actors
- [x] 7.6 Test GetQueue, GetHistory, GetConfig, GetFullStatus
- [x] 7.7 Test PauseQueue, ResumeQueue
- [x] 7.8 Test actor timeout handling in queue service

## 8. Verify

- [x] 8.1 Run `dotnet build src/FunkArr.slnx`
- [x] 8.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 8.3 Run all test projects
