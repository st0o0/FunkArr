# Tasks

## 1. Remove UI-only SABnzbd modes

- [x] **1.1** `SabnzbdController.cs`: Remove `pause`, `resume` cases from GET handler
- [x] **1.2** `SabnzbdController.cs`: Remove `priority`, `switch` cases from queue name sub-dispatch
- [x] **1.3** `SabnzbdDownloadService.cs`: Remove `SetPriority()`, `Pause()`, `Resume()`, `SwapDownloads()` methods
- [x] **1.4** `SabnzbdQueueService.cs`: Remove `MapPriority()` call from queue slot building, hardcode `SabnzbdPriority.Normal`
- [x] **1.5** `SabnzbdResponseMapper.cs`: Remove `MapSabnzbdPriority()` and `MapPriority()` methods

## 2. Remove SabnzbdPriority enum

- [x] **2.1** Delete `SabnzbdPriority` from `SabnzbdEnums.cs` (or keep as dead code if `QueueSlot` record still references it — update `QueueSlot.Priority` to always emit `"Normal"`)
- [x] **2.2** Remove `JsonStringEnumConverter<SabnzbdPriority>` registration if separate from enum

## 3. Remove DownloadPriority.Paused from domain

- [x] **3.1** `DownloadPriority.cs` (Messages): Remove `Paused = 1` member
- [x] **3.2** `PersistedDownloadPriority` (Persistence): Remove `Paused = 1` member
- [x] **3.3** `PersistenceMapping.cs` (Download): Map old `(PersistedDownloadPriority)1` → `Low` in `ToDomain()` for snapshot recovery
- [x] **3.4** `DownloadManager.cs`: Remove `Priority != DownloadPriority.Paused` check from `DispatchNext()`
- [x] **3.5** `EnumMappingExtensions.cs` (Api): Remove `Paused` arms from `QueuePriority` ↔ `DownloadPriority` mapping
- [x] **3.6** `QueuePriority` enum (Api): Remove `Paused = 1` member

## 4. Internal API priority validation

- [x] **4.1** `DownloadsApiEndpoints.cs`: Add guard in priority endpoint — reject Unknown (0) and old Paused (1) with 400 ProblemDetails
- [x] **4.2** `DownloadQueueResponse.cs`: Update `QueuePriority` enum doc comment to show valid values

## 5. Frontend error handling

- [x] **5.1** `rulesets.ts`: Update `throwIfNotOk()` to extract ProblemDetails `detail` field for all 4xx responses, not just 422

## 6. Update tests

- [x] **6.1** Remove SABnzbd tests for removed modes: pause/resume/priority/swap in `SabnzbdDownloadServiceTests`, `SabnzbdWireTests`, `SabnzbdQueueServiceTests`
- [x] **6.2** Remove `SabnzbdResponseMapperTests` priority mapping tests
- [x] **6.3** Update `EnumMappingExtensionsTests`: remove Paused arm
- [x] **6.4** Add priority validation test in `DownloadApiEndpointTests`: 400 for Unknown and Paused values
- [x] **6.5** Verify `dotnet build` and `dotnet format` pass

## 7. Update E2E test plan

- [x] **7.1** Remove SABnzbd-only tests 21.8 (priority), 21.9 (swap), 21.10 (pause), 21.11 (resume) from E2E-TEST-PLAN.md
- [x] **7.2** Update priority enum values in 25.4 and 28.8: Low=2, Normal=3, High=4 (Paused removed)
- [x] **7.3** Update e2e-verify skill subagent prompts to match
- [x] **7.4** Add test 36.5a-like test for removed SABnzbd modes → expect `{"status":false,"error":"Invalid mode"}` or similar
