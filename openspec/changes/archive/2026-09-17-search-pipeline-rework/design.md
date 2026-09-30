## Context

The search pipeline flows: Mediathek query → RuleSet resolution → MatchMagic scoring → MetadataResolver enrichment → result building. Currently this is orchestrated by TvSearchWorker/MovieSearchWorker using Ask+PipeTo with failure lambdas. The workers contain decision logic (NeedsEnrichment, conditional Become), result-building logic lives in a static SearchPipeline class with 3 overloads, and MediathekItem leaks through the entire pipeline. State is a record with extension methods. The result is a flat SearchResultItem carrying data from all stages with many optional fields.

Reference implementation: D:\GIT\Akka.Pathfinder PointWorker — state class with TryGet methods that make routing decisions and return ready-to-send messages, workers are pure orchestrators.

## Goals / Non-Goals

**Goals:**
- Typed pipeline stages: each stage has its own record type (SourceInfo → EnrichedItem → ReleaseVariant)
- Composed sub-records (MediaIdentity, MatchInfo) to group related fields instead of flat bags
- State as class with Apply() for transformations and TryGet() for routing decisions
- Tell+Timer instead of Ask+PipeTo+failure lambdas
- Workers become pure orchestrators: receive → state.Apply → tell/reply
- Delete SearchPipeline.cs and SearchContext

**Non-Goals:**
- Changing MetadataResolver internals (EpisodeResolver, MovieResolver, actors)
- Changing message types in FunkArr.Messages.MetadataResolver
- Changing SearchManager orchestration
- Changing the Newznab/SABnzbd adapter layer
- Persistence changes

## Decisions

### Decision 1: State is a class, not a record

**Choice:** Mutable class with Apply methods and private setters.

**Why:** The Apply pattern (`_state.Apply(msg)`) reads cleaner than `_state = _state.Apply(msg)`. The state is never shared — it lives on a single actor. Records with extension methods add ceremony without immutability benefits since the actor mutates `_state` anyway. Matches the Pathfinder project's established pattern.

**Alternative:** Keep records with extension methods. Rejected because it requires reassignment on every Apply and prevents methods like TryGet from reading internal state naturally.

### Decision 2: Pipeline types live in FunkArr.Search

**Choice:** SourceInfo, MediaIdentity, MatchInfo, EnrichedItem, ReleaseVariant are internal to FunkArr.Search. They never appear in messages.

**Why:** These are pipeline-internal transformation types. They don't cross actor boundaries — only messages do. Keeping them in Search avoids polluting the Messages project with implementation details.

**Alternative:** Put shared types (MediaIdentity, MatchInfo) in Messages. Rejected because nothing outside Search needs them.

### Decision 3: Tell+Timer replaces Ask+PipeTo

**Choice:** All inter-actor communication uses Tell with explicit timer-based timeouts. Workers implement `IWithTimers`.

**Why:** Ask creates hidden Tasks that can fail silently. PipeTo with failure lambdas scatters error handling. Tell+Timer is explicit: send message, set timer, handle response or timeout — one code path each. Timer keys are named by phase (e.g., "mediathek-timeout", "scoring-timeout", "enrich-timeout").

**Alternative:** Keep Ask+PipeTo. Rejected because it mixes async Task machinery with actor message handling.

### Decision 4: State TryGet methods build outgoing messages

**Choice:** State has TryGet methods that return ready-to-send actor messages: `TryGetEnrichmentRequest(out EnrichEpisodes?)`, `TryGetScoringRequest(out ScoreItems?)`, `TryGetRuleSetRequest(out ResolveRuleSet?)`, `TryGetMediathekQuery(out QueryMediathek?)`.

**Why:** Follows Pathfinder PointWorker pattern. The state has all the data to decide IF a request is needed AND to build the message. The worker just sends it. No separate NeedsX + BuildX methods.

**Alternative:** Worker builds messages from state properties. Rejected because it leaks decision logic into the worker.

### Decision 5: EnrichedItem is the central pipeline type

**Choice:** After scoring, state holds EnrichedItem[]. Items without enrichment have Match=null. No separate ScoredItem type.

**Why:** ScoredItem would only exist transiently between scoring and the enrichment decision. Since Apply(ScoreCompleted) can directly produce EnrichedItem[] (with Match=null), an intermediate type adds no value. The enrichment step patches existing items rather than transforming to a new type.

**Alternative:** Separate ScoredItem and EnrichedItem types. Rejected as unnecessary indirection — the enrichment step is a patch, not a transformation.

### Decision 6: SourceInfo replaces MediathekItem in the pipeline

**Choice:** Apply(MediathekQueryCompleted) projects MediathekItem[] to SourceInfo[]. All subsequent pipeline steps work with SourceInfo. VideoQuality.GetVariants takes SourceInfo.

**Why:** MediathekItem is a JSON deserialization target with field names matching the external API. SourceInfo is a domain projection with clean field names and DateTimeOffset instead of unix timestamp.

### Decision 7: ReleaseVariant.ToResultItem() for API mapping

**Choice:** ReleaseVariant has a ToResultItem() method that produces the flat SearchResultItem DTO.

**Why:** SearchResultItem is correct as a flat API DTO. The mapping is trivial 1:1 field copying — no logic. Having it on ReleaseVariant keeps the mapping co-located with the source type.

### Decision 8: Timeout messages are per-phase private records

**Choice:** Each worker defines private timeout records: `MediathekTimeout`, `RuleSetTimeout`, `ScoringTimeout`, `EnrichmentTimeout`.

**Why:** Typed timeouts allow Receive dispatch. The timer key and the message type together identify which phase timed out. Private because they never leave the actor.

## Risks / Trade-offs

**[Tell+Timer increases message count]** → Each phase now has a timeout message type and timer management. Mitigated by consistent pattern: every phase follows the same Tell/Timer/Cancel template.

**[State class loses record benefits]** → No with-expressions, no structural equality. Mitigated by: state is never compared or cloned, and Apply methods are more readable than extension methods returning new records.

**[Large state class]** → TvSearchWorkerState will have ~8 Apply methods + ~4 TryGet methods + ToSearchCompleted. Mitigated by: each method is small and focused. Can use partial class if file gets long.

**[Breaking change to test patterns]** → Tests currently use Ask to drive workers. With Tell+Timer, tests need TestProbe for ReplyTo and manual timer advancement. Mitigated by: xUnit test helpers can wrap the pattern.
