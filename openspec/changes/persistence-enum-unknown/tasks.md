## 1. Persistence Enums

- [x] 1.1 Add `Unknown = 0` to all 11 `Persisted*` enums and renumber existing members starting at 1. `PersistedDownloadPriority`: replace `Paused=-2..High=1` with `Unknown=0, Paused=1, Low=2, Normal=3, High=4`. `PersistedSearchSource`: preserve relative gap (`Unknown=0, Sonarr=1, Radarr=2, Prowlarr=3, Feed=5`).

## 2. Messages Enums

- [x] 2.1 Add `Unknown = 0` to `DownloadPhase`, `DownloadStatus`, `FailureKind`, `WorkerStatus` in `FunkArr.Messages/Download/` and renumber existing members from 1.
- [x] 2.2 Renumber `DownloadPriority` to `Unknown=0, Paused=1, Low=2, Normal=3, High=4`.
- [x] 2.3 Add `Unknown = 0` to `CreateLocalRuleSetFailureReason`, `UpdateLocalRuleSetFailureReason`, `DeleteLocalRuleSetFailureReason`, `ExportRuleSetFailureReason` in `FunkArr.Messages/RuleSet/LocalRuleSetCommands.cs` and renumber existing members from 1.

## 3. Api Enums

- [x] 3.1 Renumber `QueuePriority` in `FunkArr.Api` to `Unknown=0, Paused=1, Low=2, Normal=3, High=4`.

## 4. Mapping Extensions

- [x] 4.1 Update all Persistence↔Messages mapping extensions in domain projects (`FunkArr.Download`, `FunkArr.Search`, `FunkArr.History`, `FunkArr.Scoring`, `FunkArr.Feed`, `FunkArr.Enrichment`) to use exhaustive switch with new values. Verify `Unknown` falls through to `_ => throw`.
- [x] 4.2 Update all Messages↔Api mapping extensions in `FunkArr.Api/Extensions/` for new values. Verify `Unknown` falls through to `_ => throw`.
- [x] 4.3 Update SABnzbd priority mapping in `FunkArr.ArrApi` for new `DownloadPriority` values.
- [x] 4.4 Verify no code uses `(int)DownloadPriority` or `(int)PersistedDownloadPriority` for sorting or comparison. Fix any found.

## 5. Tests

- [x] 5.1 Update test assertions on enum values across all test projects (numeric comparisons, mapping round-trip tests, persistence DTO tests).
- [x] 5.2 Build solution (`dotnet build src/FunkArr.slnx`) — fix any CS8509 (missing switch arm) or other compile errors.
- [x] 5.3 Run all test projects and fix failures.
- [x] 5.4 Run `dotnet format src/FunkArr.slnx --verify-no-changes` and fix any violations.
