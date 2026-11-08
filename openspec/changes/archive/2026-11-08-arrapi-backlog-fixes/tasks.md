## 1. Daily Show Episode Matching

- [x] 1.1 Add daily-show branch to `FilterBySeasonEpisode` in `NewznabSearchService.cs`: when `season >= 1900`, parse `ep` as MMDD, construct date from year+MMDD, compare against `item.AiredAt?.Date`
- [x] 1.2 Add tests for daily-show filtering: matching airdate, non-matching airdate, null AiredAt, invalid MMDD, regular season/episode unchanged

## 2. History Trim Safety

- [x] 2.1 Add `TimeProvider` parameter to `TrimIfNeeded` in `DownloadHistoryManagerState.cs`, skip records where `CompletedAt` is within last 30 minutes
- [x] 2.2 Pass `TimeProvider` from `DownloadHistoryManager` actor to `TrimIfNeeded` call in `HandleRecord`
- [x] 2.3 Add tests for age-based trim: mixed old/recent records, all recent (no trim), all old (normal trim), edge case at 30-minute boundary

## 3. Verify

- [x] 3.1 Run `dotnet build src/FunkArr.slnx` — zero warnings
- [x] 3.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 3.3 Run affected test projects (ArrApi.Tests, Download.Tests)
- [x] 3.4 Update delta specs in `openspec/` to reflect final implementation
