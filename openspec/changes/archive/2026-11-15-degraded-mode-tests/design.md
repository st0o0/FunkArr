# Design: Degraded Mode Tests

## Approach

### MVW Queue Saturation (D.1.3)
Test `MediathekViewWebManager` actor: send >64 queries simultaneously, verify
overflow queries receive `QueryMediathekQueueFull`. Uses TestKit with a slow mock
`MediathekClient`.

### RuleSet Updater Degraded (D.4.1, D.4.2)
Test `RuleSetUpdater` actor with mocked `IHttpClientFactory`:
- GitHub returns 500 → logs warning, schedules next check, rulesets unchanged
- GitHub returns 403 → same behavior, no crash

### FFmpeg Crash (D.5.3)
Test DownloadWorker state transitions: apply events simulating FFmpeg crash scenario.
Verify worker reaches Failed status with correct failure info.

### Download Worker Failure State (D.5.3 extended)
State-level tests verifying:
- Worker state after initialization + fault = Failed
- Worker state has correct fail message
- Worker state tracks failure kind
