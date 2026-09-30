## Context

SearchWorkers (TvSearchWorker, MovieSearchWorker) are sharded entities that process a single search request through a multi-step pipeline: query Mediathek → resolve RuleSet → score items → enrich metadata → reply. Currently they use a flat ReceiveActor with all handlers registered in the constructor, null-check guards on every handler, manual Sender threading via PipeTo, and manual Passivate calls on every exit path. The RuleSet response types use a marker interface (`IRuleSetResponse`) and `RuleSetNotFound` instead of the exception-based failure pattern used by the enrichment domain.

## Goals / Non-Goals

**Goals:**
- Explicit phase tracking via Become — each phase handles only its messages
- ReplyTo captured once in state, eliminating PipeTo(Self, Sender) threading
- ScoredResults in state instead of dangling field
- Auto-passivation via ShardOptions for search workers
- RuleSet failure type aligned with enrichment pattern
- Response marker interfaces replaced by abstract records (RuleSet + enrichment)

**Non-Goals:**
- Changing the search pipeline logic or flow
- Touching DownloadWorker or MatchHistoryWorker passivation
- Converting all 7 response interfaces (ISearchResponse, IMediathekResponse, IScoringResponse, IDownloadResponse stay as-is)
- Adding new search features or capabilities

## Decisions

### D1: Become phases for SearchWorkers

The worker starts in a constructor that only handles the initial command (TvSearch/MovieSearch). Each transition calls Become to switch to the next phase's handler set.

Phases:
- **Constructor**: Receives TvSearch/MovieSearch, captures ReplyTo, routes to first step
- **Querying**: Handles MediathekQueryCompleted/Failed
- **ResolvingRuleSet**: Handles RuleSetResolved/RuleSetFailed
- **Scoring**: Handles ScoreCompleted/ScoringFailed
- **Enriching**: Handles EpisodesEnriched/EpisodeEnrichmentFailed (TV) or MoviesEnriched/MovieEnrichmentFailed (Movie)

The early pipeline is not always linear — depending on inputs, the worker may go Querying → ResolvingRuleSet or ResolvingRuleSet → Querying. This is fine: the Become handlers know which transitions are valid from their phase.

After each Become, the worker only accepts messages relevant to that phase. Stale messages from earlier phases are ignored or logged.

### D2: State record with ReplyTo and ScoredResults

```
TvSearchWorkerState
├── SearchId: Guid
├── ReplyTo: IActorRef          ← captured once from Sender on TvSearch
├── Source: string
├── RawItems: MediathekItem[]
├── RuleSetId: string?
├── TvdbId: int?
├── ImdbId: string?
├── Season: int?
├── MediaName: string?
├── ScoredResults: ScoreCompleted?  ← moved from dangling field
```

All reply calls use `_state.ReplyTo.Tell(...)` instead of `Sender.Tell(...)`.
All PipeTo calls become `PipeTo(Self)` — no Sender argument needed.

**Why not pass ReplyTo through messages?** The worker is single-use — one command, one reply target. Storing it once in state is simpler than embedding it in every internal message.

### D3: PassivateIdleEntityAfter for search shards

```csharp
new ShardOptions { PassivateIdleEntityAfter = TimeSpan.FromSeconds(30) }
```

Search workers are single-use: they process one request and reply. After replying, they go idle. The shard automatically passivates them after 30 seconds of inactivity.

This eliminates 7 manual `Context.Parent.Tell(new Passivate(PoisonPill.Instance))` calls per worker (14 total). The 30-second window is generous — the worker is lightweight after responding (no connections, just a state record in memory).

**Why not shorter?** The worker could be reused if the shard routes another message with the same SearchId before passivation. This is unlikely but harmless — the worker would just ignore it (wrong phase).

**Why only search shards?** DownloadWorker has cleanup logic (CancelRunning) in its Passivate helper. MatchHistoryWorker uses ReceiveTimeout with specific behavior. Both need explicit passivation control.

### D4: RuleSetFailed with RuleSetNotFoundException

```csharp
public sealed class RuleSetNotFoundException(string topicOrAlias)
    : Exception($"RuleSet not found for '{topicOrAlias}'")
{
    public string TopicOrAlias { get; } = topicOrAlias;
}

public abstract record RuleSetResponse;
public sealed record RuleSetResolved(string RuleSetId, string Topic, string? MediaName = null) : RuleSetResponse;
public sealed record RuleSetFailed(Exception Cause) : RuleSetResponse;
```

Benefits:
- PipeTo failure handlers become `failure: ex => new RuleSetFailed(ex)` — real exceptions flow through instead of being swallowed into `RuleSetNotFound(query)`
- Callers can distinguish "not found" from "resolver crashed" via exception type
- `RuleSetResolverState.Resolve()` return type changes from `object` to `RuleSetResponse`

### D5: Abstract records for response types

Convert these marker interfaces to abstract records:
- `IRuleSetResponse` → `abstract record RuleSetResponse`
- `IEpisodeEnrichmentResponse` → `abstract record EpisodeEnrichmentResponse`
- `IMovieEnrichmentResponse` → `abstract record MovieEnrichmentResponse`

**Why abstract record over interface?**
- Records can only inherit from records — compiler enforces the hierarchy
- Pattern matching on abstract records gives exhaustiveness hints
- `RuleSetResponse` reads more naturally than `IRuleSetResponse`
- The "is-a" relationship makes domain sense

Other response interfaces (`ISearchResponse`, `IMediathekResponse`, `IScoringResponse`, `IDownloadResponse`) stay as-is — they're not in scope and can be migrated independently.

## Risks / Trade-offs

- **[Become complexity]** Become-based phases add more methods but each is simpler. Net effect is positive for readability. → Mitigation: each phase method is short and focused.
- **[30s idle window]** Search workers stay alive 30s after responding. → Mitigation: they're lightweight (one state record, no connections). Memory impact is negligible.
- **[RuleSetFailed in API]** The API endpoint pattern-matches `RuleSetNotFound => Results.NotFound()`. With `RuleSetFailed`, it needs `RuleSetFailed { Cause: RuleSetNotFoundException } => Results.NotFound()`. → Mitigation: simple pattern match change, more correct (distinguishes not-found from crash).
