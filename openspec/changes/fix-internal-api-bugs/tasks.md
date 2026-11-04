## 1. Ask error mapping

- [x] 1.1 `EndpointExceptionFilter`: catch `AskTimeoutException` → `ApiResults.GatewayTimeout()` with warning log (same as `TimeoutException`)
- [x] 1.2 Add `ApiResults.UnexpectedResponse(...)` (throws `UnexpectedActorResponseException`; the filter logs the response type and returns 500); replace every `_ => ApiResults.GatewayTimeout()` and `_ => Results.Problem("Unexpected response", ...)` default arm in `*ApiEndpoints.cs`
- [x] 1.3 Tests: filter maps `AskTimeoutException` to 504; endpoint with an unhandled reply type returns 500 (TestProbe replying an unrelated message)

## 2. Log stream fan-out

- [x] 2.1 `RingBufferSink`: per-subscriber bounded channels (`Subscribe(ct)`, 256, DropOldest), removal on cancel, `Emit` fans out; remove `Reader`
- [x] 2.2 `/api/system/logs/stream` uses `sink.Subscribe(ctx.RequestAborted)`
- [x] 2.3 Tests: two subscribers each receive all entries in order; entries emitted with no subscriber are not delivered to a later subscriber; ring capacity 500; a non-reading subscriber does not block `Emit` or other subscribers

## 3. Retry ordering

- [x] 3.1 `DownloadManager.HandleRetry`: tell `IDownloadHistoryManager` `RemoveHistoryEntry` inside the `DownloadEnqueued` Persist callback
- [x] 3.2 Retry endpoint: remove the `history.Tell(new RemoveHistoryEntry(id))` and the history manager resolution
- [x] 3.3 Tests: DownloadManager accepted retry sends `RemoveHistoryEntry` to a registered TestProbe; rejected retry sends nothing; integration test: failed retry keeps the history entry

## 4. Verification

- [x] 4.1 `dotnet build src/FunkArr.slnx`, `dotnet format src/FunkArr.slnx --verify-no-changes`, Api/Download/Integration test projects green (also fixed pre-existing `IDE1006` in `MovieSearchWorker.cs` and unused `MatchExtractions` import in `rulesetMatcher.test.ts` that broke format check and UI build)
- [x] 4.2 Dev stack: two concurrent `/api/system/logs/stream` clients both receive the same live entries (280 each, 279 shared in order; the difference is each client's own connect/disconnect log)
