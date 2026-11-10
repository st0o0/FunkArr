## 1. Remove stale integration tests

- [x] 1.1 Delete `Priority_Success_ReturnsOk` method from `src/FunkArr.IntegrationTests/Api/DownloadReorderTests.cs` (lines 47-67)
- [x] 1.2 Delete `Pause_returns_success` method from `src/FunkArr.IntegrationTests/Arr/SabnzbdDownloadTests.cs` (lines 35-51)
- [x] 1.3 Delete `Resume_returns_success` method from `src/FunkArr.IntegrationTests/Arr/SabnzbdDownloadTests.cs` (lines 53-69)
- [x] 1.4 Delete `Queue_priority_returns_success` method from `src/FunkArr.IntegrationTests/Arr/SabnzbdQueueTests.cs` (lines 62-80)
- [x] 1.5 Delete `Queue_swap_returns_success` method from `src/FunkArr.IntegrationTests/Arr/SabnzbdQueueTests.cs` (lines 82-103)

## 2. Sync main specs with codebase

- [x] 2.1 Update `openspec/specs/download-priority/spec.md` — remove Paused enum value, SABnzbd queue priority/switch operations, per-item pause requirements, internal API pause/resume endpoints, persistence round-trip for Paused
- [x] 2.2 Update `openspec/specs/sabnzbd-download-api/spec.md` — no changes needed, spec already clean (pause/resume/value2 were in download-priority spec)
- [x] 2.3 Update `openspec/specs/integration-test-downloads/spec.md` — remove "Pause returns success" scenario
- [x] 2.4 Update `openspec/specs/download-queue-reorder/spec.md` — no changes needed, spec has no SABnzbd references (internal API endpoints remain)

## 3. Verify

- [x] 3.1 Build: `dotnet build src/FunkArr.slnx`
- [x] 3.2 Run integration tests: `dotnet run --project src/FunkArr.IntegrationTests/FunkArr.IntegrationTests.csproj`
