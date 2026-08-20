# Design: search-actor-split

## Context

`SearchActor` (`src/FunkArr/Search/SearchActor.cs`, 389 lines) currently does
all of the following in one `ReceiveActor`:

- Resolves `RuleSetRegistryActor` and `MatchLedgerActor` refs on `PreStart`,
  stashing all traffic until both are resolved, and re-resolving on
  `Terminated` (the `Resolving`/`Ready` `Become` pair).
- Owns a 55-minute in-memory cache (`Dictionary<string, CachedSearchResult>`)
  keyed per search type.
- Implements three distinct search flows inline: `HandleTvSearch` (TVDB show
  lookup, ruleset-vs-generic branching, trace building, `ApplyRuleSetMatchingWithTraces`),
  `HandleMovieSearch`, `HandleTextSearch` (both just call `MatchingPipeline.ExecuteAsync`).
- Emits `MatchRecord`s (`MatchLedgerActor.RecordMatchResult`) for both the
  ruleset path and the generic-pipeline path.

TV search is materially more complex than movie/text search (TVDB episode
lookup, air-date resolution, ruleset-vs-generic branching, per-item trace
construction) while movie and text search are near-identical thin wrappers
around `MatchingPipeline.ExecuteAsync`. As ruleset matching and TV-specific
logic grow, this 389-line actor is the wrong unit of change: touching TV
matching risks movie/text regressions, and the file is hard to reason about
as a whole.

This change is purely a decomposition: no message contract visible to
`RulesetEndpoints`, `Indexer`, or any other caller of `SearchActor` changes.
The only externally observable difference is internal actor topology.

## Goals / Non-Goals

**Goals**
- Reduce `SearchActor` to routing, caching, and dependency-resolution
  concerns only (~120 lines).
- Give each search type its own actor so TV-specific complexity (TVDB
  lookups, ruleset branching, trace building) lives separately from the
  movie/text generic-pipeline wrappers.
- Keep the external message contract (`TvSearchRequest`, `MovieSearchRequest`,
  `TextSearchRequest`, `SearchResponse`) byte-for-byte unchanged — callers
  (`Indexer`, tests) `Ask<SearchActor.SearchResponse>` exactly as before.
- Preserve existing behavior exactly: same cache keys, same cache duration,
  same match-record shape and emission conditions, same TVDB/ruleset/generic
  branching logic.

**Non-Goals**
- Not changing matching logic, scoring, cache-key format, or cache duration.
- Not making children independently addressable from outside `SearchActor`
  (no `IActorRegistry` registration for children, no direct `Ask` from
  endpoints).
- Not introducing per-request child actors — children are long-lived
  singletons created once in `PreStart`, mirroring `DownloadQueueActor`
  supervising `DownloadWorkerActor`s only where per-request lifetime is
  actually needed (it isn't here: no per-request state to isolate).
- Not changing supervision beyond Akka's default (restart) strategy — no
  custom `SupervisorStrategy` is introduced by this change.

## Decisions

### Decision: Parent keeps all actor-ref state; children are pure workers

The parent `SearchActor` retains:
- `_ruleSetRegistry` / `_matchLedger` resolution, stashing, and
  `Terminated` re-resolution (unchanged from today).
- The cache (`_cache`, `TryGetCached`, `CacheResults`).
- The only `IActorRef`s in the system pointed at `RuleSetRegistryActor` and
  `MatchLedgerActor`.

`TvSearchActor`, `MovieSearchActor`, and `TextSearchActor` hold **no actor
refs at all** — not to their parent, not to `RuleSetRegistryActor`, not to
`MatchLedgerActor`. They receive an internal `Execute*` command containing
everything they need as plain data (search term, TVDB id, pre-fetched rules,
pre-fetched TVDB episodes where applicable) and reply with a `SearchCompleted`
message back to `Sender` (the parent, since parent uses `Tell`, not `Forward`
— see below).

This is a deliberate simplification versus "child asks RuleSetRegistry
itself": it keeps inter-actor coordination (who talks to whom) entirely in
the parent, which is the one actor that already understands the system's
actor topology. Children become trivially unit-testable — construct with
mock `MediathekClient`/`TvdbClient`/`QualityProbeService`, `Tell` an
`Execute*` command, assert the `SearchCompleted` reply — with no `TestKit`
`Ask`-timeout dance around a registry actor.

### Decision: Parent resolves rules before telling TvSearchActor

Today `HandleTvSearch` does, in order: search Mediathek, `Ask` the
`RuleSetRegistry` for rules, then branch on `rules.Count > 0`. In the split,
the **parent** performs the `RuleSetRegistry.Ask<RulesResponse>` (it already
holds `_ruleSetRegistry`) and passes the resolved `IReadOnlyList<Rule>` as
part of `ExecuteTvSearch`. `TvSearchActor` never sees `RuleSetRegistryActor`
and never issues an `Ask`.

Rationale: this is the one place the original code reaches out to another
actor mid-flow. Moving that `Ask` to the parent keeps the "children touch no
actor refs" rule absolute rather than "children touch no actor refs except
this one case," which would be a harder invariant to keep true as the code
evolves.

Consequence: `TvSearchActor` still needs Mediathek search results and (for
the ruleset path) TVDB episode data before it can run
`ApplyRuleSetMatchingWithTraces` / the generic-pipeline fallback. Both of
those (`SearchMediathekAsync`, `GetTvdbEpisodesAsync`) are I/O the child
performs itself via its own `MediathekClient`/`TvdbClient` — only the
*rules* lookup, which is the one actor-to-actor call, moves to the parent.
This mirrors the proposal's framing ("parent resolves rules before telling
child") without also relocating unrelated HTTP calls that have no actor
dependency.

### Decision: Internal protocol — `Execute*` in, `SearchCompleted` out

```csharp
// SearchActor (parent) — internal, not exposed outside the file
private sealed record ExecuteTvSearch(
    string CacheKey, TvSearchRequest Request, string SearchTerm,
    string? ShowName, IReadOnlyList<Rule> Rules, IActorRef ReplyTo);

private sealed record ExecuteMovieSearch(
    string CacheKey, MovieSearchRequest Request, string SearchTerm, IActorRef ReplyTo);

private sealed record ExecuteTextSearch(
    string CacheKey, TextSearchRequest Request, IActorRef ReplyTo);

private sealed record SearchCompleted(
    string CacheKey, IReadOnlyList<SearchResult> Results,
    MatchRecord? MatchRecord, IActorRef ReplyTo);
```

`CacheKey` travels with the command so the parent doesn't have to
recompute it (or keep a `Dictionary<IActorRef-correlation, key>`) when the
`SearchCompleted` reply arrives — children are stateless and may interleave
replies for concurrent requests, so correlation must ride in the message,
not in actor state. `ReplyTo` is `Sender` at the moment the parent received
the original request; it is threaded through so the parent can `Tell` the
final `SearchResponse` back to the *original* caller even though the
`SearchCompleted` reply's `Sender` is the child, not that caller.

Parent uses `Tell` (not `Forward`) for `Execute*` commands and captures
`Sender` into `ReplyTo` explicitly — `Forward` would make the child's
implicit `Sender` the original caller, which is unnecessary here since the
child never replies directly to the caller (it always replies to the
parent with `SearchCompleted`, and the parent does the final `Tell`).

`MatchRecord? MatchRecord` covers both cases in one field: the ruleset path
always produces one (`ApplyRuleSetMatchingWithTraces`), the generic-pipeline
path also always produces one today (`EmitGenericPipelineRecord`) — so in
practice this is never `null` for `TvSearchActor`, `MovieSearchActor`, or
`TextSearchActor` as they exist today. It stays nullable because the
proposal specifies `MatchRecord?` and because it removes any temptation for
a future child (or a future branch in an existing child) to force a record
into existence when there is genuinely nothing to record.

### Decision: Cache stays entirely in the parent

Children never check or write the cache. The parent checks
`TryGetCached(cacheKey, ...)` on the incoming external request and, on a
hit, replies immediately without telling any child. On a miss, it builds the
`Execute*` command (including, for TV, the pre-fetched rules) and tells the
appropriate child. When `SearchCompleted` arrives, the parent calls
`CacheResults(msg.CacheKey, msg.Results)`, tells `_matchLedger` the
`MatchRecord` if present, and `Tell`s `msg.ReplyTo` the final
`SearchResponse`.

This keeps cache semantics (55-minute TTL, per-search-type key format)
completely unchanged and in one place — a child restart (see Supervision
below) never needs to worry about cache consistency because it never had
any.

### Decision: Children are long-lived, created once in `PreStart`

```csharp
protected override void PreStart()
{
    _tvSearchActor = Context.ActorOf(Props.Create(() =>
        new TvSearchActor(_mediathekClient, _tvdbClient, _qualityProbeService, _probeLimit)), "tv");
    _movieSearchActor = Context.ActorOf(Props.Create(() =>
        new MovieSearchActor(_mediathekClient, _qualityProbeService, _probeLimit)), "movie");
    _textSearchActor = Context.ActorOf(Props.Create(() =>
        new TextSearchActor(_mediathekClient, _qualityProbeService, _probeLimit)), "text");

    Context.GetActorAsync<RuleSetRegistryActor>().PipeTo(Self, success: r => new RuleSetRegistryResolved(r));
    Context.GetActorAsync<MatchLedgerActor>().PipeTo(Self, success: r => new MatchLedgerResolved(r));
}
```

Children are created unconditionally in `PreStart`, independent of
dependency resolution (`_ruleSetRegistry`/`_matchLedger`) — they have no
dependency on those refs, so there is no reason to gate their creation
behind the `Resolving`/`Ready` `Become` switch. The parent still stashes
*external* requests in `Resolving` exactly as today; children simply exist
and sit idle until the parent starts routing to them.

They are not per-request because there is no per-request state to isolate
(unlike `DownloadWorkerActor`, which tracks one download's progress and
must not leak state across downloads). All three children are effectively
stateless request handlers — one instance per type is sufficient, and
reusing it avoids `Props`/mailbox churn on every search.

### Decision: Default supervision (no custom strategy)

No `SupervisorStrategy` override on `SearchActor`. If `TvSearchActor` throws
an unhandled exception (e.g., malformed TVDB response), Akka's default
strategy restarts that one child; `MovieSearchActor` and `TextSearchActor`
are unaffected since they are separate children under the same parent. This
is strictly better fault isolation than today, where an unhandled exception
in TV-search code brings down the single `SearchActor` (and, with it,
movie/text search, plus the cache and dependency-resolution state) until
its own supervisor restarts it.

A crashed child loses the in-flight request's `Execute*` command (the
default strategy does not requeue the triggering message). The caller's
original `Ask` times out and receives the standard timeout failure — this
is the same externally-visible failure mode as today's actor-wide crash
producing an `Ask` timeout, just scoped to one search type instead of all
three.

### Decision: External message types and dependency-resolution logic stay on `SearchActor`

`TvSearchRequest`, `MovieSearchRequest`, `TextSearchRequest`, `SearchResponse`
remain nested `sealed record`s on `SearchActor` exactly where they are today
(`src/FunkArr/Search/SearchActor.cs`). `RulesetEndpoints`,
`FunkArrApplicationSetup`, and any other caller continue to reference
`SearchActor.TvSearchRequest` etc. — this change touches zero call sites
outside `FunkArr.Search`.

The `Resolving`/`Ready` `Become` pair, `RuleSetRegistryResolved`/
`MatchLedgerResolved`, `TryBecomeReady`, and `HandleTerminated` move
verbatim from today's `SearchActor` into the new router — no behavior
change, just relocation within the (now-router) class.

### Decision: Dependency injection for children

Children are constructed via `Props.Create(() => new XxxSearchActor(...))`
inside the parent's `PreStart`, using the same constructor-injected
dependencies the parent itself received via DI (`MediathekClient`,
`TvdbClient`, `QualityProbeService`, `IOptions<FunkArrOptions>` for
`_probeLimit`) — not by resolving them from the DI container inside
`PreStart`. `SearchActor` is itself constructed by Akka.Hosting/DI (per
`FunkArrActorSystemSetup`, unchanged by this proposal), so it already has
these dependencies in hand; passing them straight through to
`Props.Create` avoids giving children an `IServiceProvider` dependency they
don't need and keeps child construction synchronous and side-effect-free.
`TvSearchActor` additionally needs `TvdbClient`; `MovieSearchActor` and
`TextSearchActor` need only `MediathekClient` and `QualityProbeService`.

Per the proposal, this depends on `options-decomposition` for the exact
`IOptions<T>` shape passed to children (today's single `FunkArrOptions`
becomes a narrower search-specific options type per that change) and on
`shared-prefilter` for `ContentFilter`, which `MatchingPipeline`'s
`ShouldSkip` (called from all three children, directly or via
`MatchingPipeline.ExecuteAsync`) will be refactored to use. Both are
prerequisite changes; this change assumes their shapes already landed and
does not re-decide them here.

## Risks / Trade-offs

- **Extra message hop per search.** Every search now costs one additional
  `Tell` (parent to child) and one additional message (child's
  `SearchCompleted` back to parent) versus today's single method call. This
  is negligible relative to the Mediathek HTTP call and (for TV) the TVDB
  HTTP call already in the critical path, but it is a real, non-zero cost
  worth naming.
- **Rules pre-fetch changes timing slightly.** Today `HandleTvSearch` calls
  `SearchMediathekAsync` then `Ask`s for rules. In the split, the parent
  `Ask`s for rules (parent-side) while the child performs
  `SearchMediathekAsync` — the two I/O calls that were sequential in one
  method are now split across two actors and could, in principle, be
  reordered or parallelized in a later change. This change preserves
  sequential semantics (parent resolves rules, *then* tells the child, which
  then searches) rather than parallelizing, to keep the diff a pure
  decomposition with no behavior change.
- **Child crash loses in-flight request silently from the child's
  perspective.** As noted under Supervision, a crash mid-`Execute*`
  produces an `Ask` timeout at the original caller rather than an explicit
  error reply. This matches today's behavior (actor-wide crash also times
  out the caller) so it is not a regression, but it is not improved either;
  a future change could have children reply with an explicit failure
  message instead of relying on `Ask` timeout, but that is out of scope
  here.
- **Three new files/classes to navigate instead of one.** Trades a single
  389-line file for four smaller files (~120 + 130 + 40 + 30 lines per the
  proposal's estimate). This is the intended outcome, not a hidden cost, but
  it does mean tracing a TV search request now requires reading
  `SearchActor.cs` and `TvSearchActor.cs` together instead of one file.
