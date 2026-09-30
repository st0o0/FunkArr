## Why

Every cross-actor `Ask` call uses `Ask<object>` because there are no shared response interfaces — callers pattern-match on concrete types. The message interfaces are ad-hoc: `TvSearchCommand` stacks two unrelated interfaces (`ISearchCommand`, `IWithSearchId`), while `GeneralSearchCommand` implements only one. The SearchHandler in the adapter layer builds domain-internal command types (`TvSearchCommand`, `MovieSearchCommand`, `GeneralSearchCommand`) with dummy `Guid.Empty` SearchIds, leaking routing logic into the wrong layer.

## What Changes

- **Domain-level response interfaces**: one per domain (`ISearchResponse`, `IRuleSetResponse`, `IMediathekResponse`, `IScoringResponse`). Every response record implements its domain interface. All `Ask<object>` become `Ask<IDomainResponse>`.
- **Unified SearchCommand**: a single public `SearchCommand` with nested `TvParams` and `MovieParams` replaces three command types at the API boundary. The SearchHandler builds one type; the SearchManager decomposes it internally. `TvSearchCommand`/`MovieSearchCommand` become internal shard-routable messages implementing only `IWithSearchId`.
- **Delete ISearchCommand**: no longer needed — `SearchCommand` is the single public entry point. The adapter layer loses all knowledge of Tv/Movie routing.
- **Clean separation**: shard routing interfaces (`IWith*Id`) stay for `ShardMessageExtractor`. No message implements two unrelated interfaces. `RuleSetNotFound` implements `IRuleSetResponse` once, valid as a response to both `ResolveRuleSet` and `QueryRuleSetDetail`.

## Capabilities

### New Capabilities

- `message-response-interfaces`: Domain-level response marker interfaces that group success/failure response types per domain, enabling typed Ask calls

### Modified Capabilities

- `search-messages`: SearchCommand replaces three separate command types at the public API boundary; TvSearchCommand/MovieSearchCommand become shard-internal; GeneralSearchCommand and ISearchCommand deleted
- `search-gateway`: SearchManager receives unified SearchCommand instead of three separate types
- `newznab-indexer-api`: SearchHandler builds SearchCommand instead of domain-internal types
- `shard-message-contract`: TvSearchCommand/MovieSearchCommand implement only IWithSearchId (no more dual ISearchCommand + IWithSearchId)
- `ruleset-query-messages`: Response types implement IRuleSetResponse
- `match-history-queries`: Response types implement IScoringResponse

## Impact

- **FunkArr.Messages**: new response interfaces, new SearchCommand record, delete ISearchCommand and GeneralSearchCommand
- **FunkArr.ArrApi**: SearchHandler simplified — builds only SearchCommand
- **FunkArr.Search**: SearchManager receives SearchCommand, decomposes into Tv/Movie; workers use typed Ask in PipeTo
- **FunkArr.Api**: RuleSetApiEndpoints uses typed Ask (`IRuleSetResponse`, `IScoringResponse`)
- **FunkArr.Core**: ShardMessageExtractor unchanged (still matches on IWith*Id)
- **Tests**: SearchManagerTests updated for SearchCommand, worker tests updated for typed Ask
