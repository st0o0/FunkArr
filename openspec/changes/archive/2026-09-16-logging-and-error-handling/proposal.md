## Why

Production errors in API endpoints are invisible. ~18 `catch (Exception)` blocks across FunkArr.Api, FunkArr.ArrApi, and FunkArr.RuleSet swallow exceptions with zero logging. Non-actor code has no `ILogger` injected. Two PipeTo handlers in Search workers discard exceptions entirely. Actors average ~3 log lines each — happy-path flow is untraceable. Without fixing this, debugging production issues requires attaching a debugger.

## What Changes

- Inject `ILogger` into all API endpoint classes and static endpoint methods that catch exceptions
- Log every caught exception with structured Serilog templates (correlation context where available)
- Fix 2 PipeTo `failure:` handlers in `MovieSearchWorker` and `TvSearchWorker` that discard exceptions (`_ =>`)
- Add `Exception? Cause` property to `SearchFailed` message (all other failed messages already have it)
- Fix dead `field` reference in `MatchHistoryWorker.PersistenceId`
- Fix `object` return type in `MatchMagicActor` QueryDetail handler — use a proper discriminated return type
- Fix bare `catch` in `RuleSetManagerState` summary building that silently swallows errors
- Add structured logging to actors on key state transitions (message received, scoring started/completed, download state changes)

## Capabilities

### New Capabilities
- `error-handling-standards`: Standards for exception handling in API endpoints, PipeTo handlers, and actor receive blocks — what to log, how to structure it, when to rethrow vs return error responses

### Modified Capabilities
- `structured-logging`: Extend with requirements for what actors and API endpoints SHALL log (not just the Serilog infrastructure setup)
- `search-messages`: SearchFailed gains Cause property
- `match-scoring`: Fix object return type in QueryDetail, fix dead PersistenceId field
- `match-history-persistence`: Fix dead field reference in MatchHistoryWorker

## Impact

- **FunkArr.Api**: All 4 endpoint files gain ILogger injection and exception logging (~14 catch blocks)
- **FunkArr.ArrApi**: SearchHandler and SabnzbdApiEndpoints gain exception logging (~2 catch blocks)
- **FunkArr.RuleSet**: RuleSetManagerState bare catch fixed (~1 catch block)
- **FunkArr.Search**: MovieSearchWorker and TvSearchWorker PipeTo handlers fixed
- **FunkArr.MatchMagic**: MatchMagicActor QueryDetail return type, MatchHistoryWorker PersistenceId
- **FunkArr.Messages**: SearchFailed record extended with Cause
- **No API changes, no breaking changes to external consumers**
