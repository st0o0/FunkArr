## 1. Search Workers

- [x] 1.1 Replace `TimeSpan[] _retryBackoff` in `TvSearchWorker` with `static readonly BackoffPolicy` via `Backoff.Create(500ms, maxDelay: 1500ms)`, change delay lookup to `_backoff.DelayWithJitter(attempt)`
- [x] 1.2 Same replacement in `MovieSearchWorker`
- [x] 1.3 Add `using Servus.Resilience;` to both files, remove unused array

## 2. Download Worker

- [x] 2.1 Add `BackoffPolicy` instance field to `DownloadWorker`, initialize in constructor from `_optionsMonitor.CurrentValue.RetryBackoffBase` with `maxDelay: TimeSpan.FromMinutes(5)`
- [x] 2.2 Replace `CalculateBackoff(attempt)` call site with `_backoff.DelayWithJitter(attempt)`, remove `CalculateBackoff` method and `_maxBackoff` static field
- [x] 2.3 Verify attempt index semantics — current code uses `attempt - 1` in `Math.Pow`, Servus is zero-indexed. Align so attempt 0 produces the base delay.

## 3. Verify

- [x] 3.1 Build solution (`dotnet build src/FunkArr.slnx`)
- [x] 3.2 Run search tests (`dotnet run --project src/FunkArr.Search.Tests/FunkArr.Search.Tests.csproj`)
- [x] 3.3 Run download tests (`dotnet run --project src/FunkArr.Download.Tests/FunkArr.Download.Tests.csproj`)
- [x] 3.4 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
