## Context

The search workers (TvSearchWorker, MovieSearchWorker) orchestrate a multi-step pipeline: Mediathek query → RuleSet resolution → Scoring → Enrichment. Currently each step uses Tell + manual timer management (StartSingleTimer/Cancel) with 4 private timeout record types per worker. The enrichers (EpisodeEnricher, MovieEnricher) are stateless classes instantiated needlessly. SearchSeries and SearchMovie share 5 fields but have no common base.

## Goals / Non-Goals

**Goals:**
- Replace Tell+Timer with Ask+PipeTo while keeping Become-based phase transitions
- Remove IWithTimers, timeout records, and timer management from search workers
- Introduce SearchRequest abstract base for shared message fields
- Convert enrichers to static classes

**Non-Goals:**
- Unifying TvSearchWorker and MovieSearchWorker into a single actor
- Unifying TvSearchWorkerState and MovieSearchWorkerState into a single state class
- Changing the pipeline steps or their ordering
- Changing SearchManager or SearchCommand

## Decisions

### Ask+PipeTo replaces Tell+Timer within Become states

Each pipeline step changes from:

```
_actor.Tell(request);
Timers.StartSingleTimer("key", new XxxTimeout(), timeout);
Become(NextState);
```

to:

```
_actor.Ask<XxxResponse>(request, timeout)
      .PipeTo(Self, failure: ex => new XxxFailed(ex));
Become(NextState);
```

The PipeTo failure lambda maps Ask timeouts and transport errors to the domain's own Failed record type. Each Become state handles exactly two message types: XxxCompleted and XxxFailed.

**Why Ask+PipeTo over Tell+Timer:** Ask carries its own timeout and delivers failure as a message via PipeTo — no separate timer lifecycle to manage, no timer-key strings, no Cancel-on-success bookkeeping. The Become pattern stays the same, just the dispatch mechanism changes.

**Why not Status.Failure:** Status.Failure is Akka-internal. The PipeTo failure lambda converts it to the domain-owned Failed record before it reaches the mailbox.

**Alternative considered — ReceiveAsync:** Would flatten the pipeline into async/await but blocks the actor's mailbox for the duration, preventing supervision and stash patterns. Ask+PipeTo keeps the actor reactive.

### SearchRequest abstract base record

```
SearchRequest(SearchId, Source, Query, Limit, Offset) : IWithSearchId
  ├── SearchSeries(... Season, Episode, TvdbId, ImdbId)
  └── SearchMovie(... ImdbId, TmdbId)
```

State Init methods accept SearchRequest for common fields. Domain-specific fields are set via pattern match or direct property access on the concrete type.

**Why not a shared state base class:** The states differ in identity shape (TvdbId vs TmdbId), enrichment type (episodes vs movies), and query construction (topic-only vs title+topic, 300s vs 3600s duration). A base class would need generics or virtual methods — more complexity than the duplication it removes.

### Enrichers become static classes

Both EpisodeEnricher and MovieEnricher have zero fields, zero DI dependencies, and all private methods are already static. The public Resolve method is a pure function. Making the class static communicates this clearly and removes pointless instantiation.

## Risks / Trade-offs

- **[Ask creates temporary actor per call]** → Negligible overhead for search workers that process one request and then idle-passivate. Not a hot path.
- **[PipeTo failure loses Sender context]** → Not relevant here — workers capture ReplyTo in state during Init, never use Sender after that.
- **[SearchRequest base adds inheritance]** → Single level, sealed leaves, no virtual dispatch. Records handle this cleanly.
