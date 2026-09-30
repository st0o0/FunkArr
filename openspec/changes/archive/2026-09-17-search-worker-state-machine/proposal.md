## Why

SearchWorkers have three structural issues: (1) implicit state machine via null-checks — every handler starts with `if (_state is null) return;` and `_scoredResults` dangles as a mutable field outside state, (2) `Sender` is threaded through every `PipeTo(Self, Sender)` call — miss one and the response goes to deadLetters, and (3) every exit path has a manual `Context.Parent.Tell(new Passivate(PoisonPill.Instance))` call (7 per worker, 14 total). Additionally, the `RuleSetNotFound` message and response marker interfaces are inconsistent with the enrichment naming pattern established in the resolution-to-enrichment change.

## What Changes

- **BREAKING**: Refactor TvSearchWorker and MovieSearchWorker into Become-based state machines with explicit phase enum (Querying, ResolvingRuleSet, Scoring, Enriching)
- **BREAKING**: Add `ReplyTo: IActorRef` to worker state, captured once from Sender — eliminates all `PipeTo(Self, Sender)` threading
- **BREAKING**: Move `_scoredResults` into worker state as `ScoredResults: ScoreCompleted?`
- **BREAKING**: Replace manual Passivate calls with `PassivateIdleEntityAfter` on tv-search and movie-search ShardOptions
- **BREAKING**: Rename `RuleSetNotFound` to `RuleSetFailed(Exception Cause)` with new `RuleSetNotFoundException` custom exception
- **BREAKING**: Replace `IRuleSetResponse` marker interface with `abstract record RuleSetResponse`
- **BREAKING**: Replace `IEpisodeEnrichmentResponse` with `abstract record EpisodeEnrichmentResponse`
- **BREAKING**: Replace `IMovieEnrichmentResponse` with `abstract record MovieEnrichmentResponse`

## Capabilities

### New Capabilities

_(none)_

### Modified Capabilities

- `tv-search`: Become-based state machine, ReplyTo in state, auto-passivation, RuleSetFailed instead of RuleSetNotFound
- `movie-search`: Same treatment as tv-search
- `search-messages`: RuleSetResponse abstract record, RuleSetFailed, enrichment response abstract records
- `episode-resolution-messages`: IEpisodeEnrichmentResponse becomes abstract record EpisodeEnrichmentResponse
- `movie-resolution-messages`: IMovieEnrichmentResponse becomes abstract record MovieEnrichmentResponse

## Impact

- **FunkArr.Search**: Both workers refactored to Become phases, Passivate removed
- **FunkArr.Messages.RuleSet**: IRuleSetResponse → abstract record, RuleSetNotFound → RuleSetFailed + RuleSetNotFoundException
- **FunkArr.Messages.MetadataResolver**: Enrichment response interfaces → abstract records
- **FunkArr/Configuration**: ShardOptions updated for search regions
- **FunkArr.RuleSet**: RuleSetResolver updated for new failure type
- **FunkArr.MetadataResolver**: Resolver actors updated for new response types
- **FunkArr.Api**: RuleSetApiEndpoints updated for new response types
- **Tests**: All search, ruleset, and metadata resolver tests updated
- **No wire/API impact**: All changes are internal message types
