## 1. Cross-Domain Consistency Tests (E.3)

- [x] 1.1 Add `StateConsistencyTests.cs` in IntegrationTests with download queue count cross-check between internal API and SABnzbd API (E.3.4)

## 2. Queue Performance Tests (H.1)

- [x] 2.1 Add `DownloadManagerQueueScaleTests.cs` in Download.Tests with: 50-item mixed-priority ordering test (H.1.2)
- [x] 2.2 Add ForceStart-while-at-capacity test verifying dispatched exceeds MaxConcurrent (H.1.3)
- [x] 2.3 Add rapid enqueue/dequeue 100 items test verifying no corruption (H.1.4)

## 3. Verify

- [x] 3.1 Run `dotnet build src/FunkArr.slnx` — zero errors
- [x] 3.2 Run integration tests — all pass
- [x] 3.3 Run Download.Tests — all pass
