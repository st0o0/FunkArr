## 1. RuleSet response types

- [x] 1.1 Create `RuleSetNotFoundException` in `FunkArr.Messages/RuleSet/RuleSetNotFoundException.cs` — sealed class extending Exception, with `TopicOrAlias` property
- [x] 1.2 Replace `IRuleSetResponse` interface with `abstract record RuleSetResponse` — file: `FunkArr.Messages/RuleSet/IRuleSetResponse.cs`
- [x] 1.3 Rename `RuleSetNotFound` to `RuleSetFailed(Exception Cause)` extending `RuleSetResponse` — file: `FunkArr.Messages/RuleSet/RuleSetNotFound.cs`
- [x] 1.4 Update `RuleSetResolved` to extend `RuleSetResponse` instead of implementing `IRuleSetResponse` — file: `FunkArr.Messages/RuleSet/RuleSetResolved.cs`
- [x] 1.5 Update `RuleSetResolverState.Resolve()` return type from `object` to `RuleSetResponse`, use `RuleSetFailed(new RuleSetNotFoundException(...))` — file: `FunkArr.RuleSet/RuleSetResolverState.cs`
- [x] 1.6 Update `RuleSetApiEndpoints` — `Ask<RuleSetResponse>`, pattern match `RuleSetFailed { Cause: RuleSetNotFoundException }` — file: `FunkArr.Api/RuleSetApiEndpoints.cs`

## 2. Enrichment response abstract records

- [x] 2.1 Replace `IEpisodeEnrichmentResponse` with `abstract record EpisodeEnrichmentResponse`, update `EpisodesEnriched` and `EpisodeEnrichmentFailed` to extend it — file: `FunkArr.Messages/MetadataResolver/IEpisodeEnrichmentResponse.cs`
- [x] 2.2 Replace `IMovieEnrichmentResponse` with `abstract record MovieEnrichmentResponse`, update `MoviesEnriched` and `MovieEnrichmentFailed` to extend it — file: `FunkArr.Messages/MetadataResolver/IMovieEnrichmentResponse.cs`
- [x] 2.3 Update `TvdbResolverActor` — `EpisodesEnriched`/`EpisodeEnrichmentFailed` already extend new base (no actor changes expected, verify compile)
- [x] 2.4 Update `TmdbResolverActor` — same verification

## 3. SearchWorker state with ReplyTo and ScoredResults

- [x] 3.1 Add `ReplyTo: IActorRef` and `ScoredResults: ScoreCompleted?` to `TvSearchWorkerState` — file: `FunkArr.Search/TvSearchWorkerState.cs`
- [x] 3.2 Add `ReplyTo: IActorRef` and `ScoredResults: ScoreCompleted?` to `MovieSearchWorkerState` — file: `FunkArr.Search/MovieSearchWorkerState.cs`

## 4. TvSearchWorker Become refactor

- [x] 4.1 Refactor `TvSearchWorker` — capture ReplyTo from Sender on TvSearch, use Become for phases (Querying, ResolvingRuleSet, Scoring, Enriching), replace all `Sender.Tell(...)` with `_state.ReplyTo.Tell(...)`, replace all `PipeTo(Self, Sender)` with `PipeTo(Self)`, remove all `Context.Parent.Tell(new Passivate(...))` calls, remove `_scoredResults` field (use state), remove null-check guards (Become handles phase gating)

## 5. MovieSearchWorker Become refactor

- [x] 5.1 Refactor `MovieSearchWorker` — same treatment as TvSearchWorker: ReplyTo, Become phases, PipeTo(Self), no Passivate, ScoredResults in state, handle `RuleSetFailed` instead of `RuleSetNotFound`

## 6. ShardOptions PassivateIdleEntityAfter

- [x] 6.1 Set `PassivateIdleEntityAfter = TimeSpan.FromSeconds(30)` on tv-search and movie-search ShardOptions — file: `FunkArr/Configuration/AkkaSetupContainer.cs`

## 7. Update tests

- [x] 7.1 Update `TvSearchWorkerTests` — `RuleSetNotFound` → `RuleSetFailed(new RuleSetNotFoundException(...))`, remove any Passivate expectations if present
- [x] 7.2 Update `MovieSearchWorkerTests` — same as TvSearchWorkerTests
- [x] 7.3 Update `RuleSetResolverTests` — adapt assertions to `RuleSetFailed`/`RuleSetNotFoundException`
- [x] 7.4 Update `RuleSetManagerTests` — adapt assertions to `RuleSetFailed` if referenced
- [x] 7.5 Update `MetadataResolverManagerTests` — verify enrichment abstract records compile

## 8. Rename files and clean up

- [x] 8.1 Rename `IRuleSetResponse.cs` → `RuleSetResponse.cs`, `RuleSetNotFound.cs` → `RuleSetFailed.cs`, `IEpisodeEnrichmentResponse.cs` → `EpisodeEnrichmentResponse.cs`, `IMovieEnrichmentResponse.cs` → `MovieEnrichmentResponse.cs`

## 9. Build and format

- [x] 9.1 Run `dotnet build src/FunkArr.slnx` — verify clean compile
- [x] 9.2 Run `dotnet format src/FunkArr.slnx` — fix formatting
- [x] 9.3 Run all affected test projects — verify green
