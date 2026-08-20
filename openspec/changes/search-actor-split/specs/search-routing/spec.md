## ADDED Requirements

### Requirement: Request routing to dedicated child actors
`SearchActor` SHALL forward each incoming search request to a dedicated
child actor based on request type: `TvSearchRequest` to `TvSearchActor`,
`MovieSearchRequest` to `MovieSearchActor`, `TextSearchRequest` to
`TextSearchActor`.

#### Scenario: TV search routed to TvSearchActor
- **WHEN** `SearchActor` receives a `TvSearchRequest` and no cached result exists
- **THEN** `SearchActor` SHALL send an internal `ExecuteTvSearch` command to `TvSearchActor`

#### Scenario: Movie search routed to MovieSearchActor
- **WHEN** `SearchActor` receives a `MovieSearchRequest` and no cached result exists
- **THEN** `SearchActor` SHALL send an internal `ExecuteMovieSearch` command to `MovieSearchActor`

#### Scenario: Text search routed to TextSearchActor
- **WHEN** `SearchActor` receives a `TextSearchRequest` and no cached result exists
- **THEN** `SearchActor` SHALL send an internal `ExecuteTextSearch` command to `TextSearchActor`

### Requirement: Cache checked before routing, populated from child results
`SearchActor` SHALL check its cache for a matching cache key before routing a
request to any child actor. When a child actor returns a `SearchCompleted`
result, `SearchActor` SHALL store the results in its cache under the cache
key carried on that request.

#### Scenario: Cache hit skips child dispatch
- **WHEN** `SearchActor` receives a request whose cache key has a non-expired
  cached entry
- **THEN** `SearchActor` SHALL reply with the cached results and SHALL NOT
  send any command to a child actor

#### Scenario: Cache miss dispatches to child and caches the reply
- **WHEN** `SearchActor` receives a request whose cache key has no
  non-expired cached entry
- **THEN** `SearchActor` SHALL dispatch the request to the appropriate child
  actor, and upon receiving `SearchCompleted` SHALL store the results under
  that cache key before replying to the original sender

#### Scenario: Cache duration and key format unchanged
- **WHEN** a search result is cached
- **THEN** it SHALL expire after 55 minutes, using the same per-search-type
  cache key format as before this change (`tv:{tvdbId}:{season}:{episode}:{term}`,
  `movie:{imdbId}:{term}`, `text:{query}`)

### Requirement: Dependency resolution stays in the parent
`SearchActor` SHALL resolve `IActorRef`s for `RuleSetRegistryActor` and
`MatchLedgerActor` on startup, SHALL stash all incoming search requests
until both are resolved, and SHALL re-resolve either ref if the
corresponding actor terminates.

#### Scenario: Requests stashed until dependencies resolved
- **WHEN** `SearchActor` starts and has not yet resolved both
  `RuleSetRegistryActor` and `MatchLedgerActor` refs
- **THEN** any `TvSearchRequest`, `MovieSearchRequest`, or `TextSearchRequest`
  received SHALL be stashed and not routed to any child

#### Scenario: Stashed requests unstashed once both dependencies resolved
- **WHEN** both `RuleSetRegistryActor` and `MatchLedgerActor` refs have been
  resolved
- **THEN** `SearchActor` SHALL unstash and process all previously stashed
  requests

#### Scenario: Re-resolution on dependency termination
- **WHEN** the actor behind the resolved `RuleSetRegistryActor` ref (or the
  `MatchLedgerActor` ref) terminates
- **THEN** `SearchActor` SHALL clear that ref, transition back to resolving,
  and re-request the ref, stashing new requests until it is resolved again

### Requirement: Rules pre-fetched by parent for TV search
Before sending `ExecuteTvSearch` to `TvSearchActor`, `SearchActor` SHALL ask
`RuleSetRegistryActor` for the rules applicable to the search topic and
include the resolved rules in the `ExecuteTvSearch` command.
`TvSearchActor` SHALL NOT hold or use a reference to `RuleSetRegistryActor`.

#### Scenario: Rules resolved before child dispatch
- **WHEN** `SearchActor` routes a `TvSearchRequest` to `TvSearchActor`
- **THEN** `SearchActor` SHALL have already asked `RuleSetRegistryActor` for
  rules matching the search topic and TVDB id, and SHALL include the
  resulting rule list in the `ExecuteTvSearch` command

#### Scenario: Empty rule list still dispatched
- **WHEN** `RuleSetRegistryActor` returns no rules for the topic
- **THEN** `SearchActor` SHALL still send `ExecuteTvSearch` to
  `TvSearchActor` with an empty rule list, and `TvSearchActor` SHALL fall
  back to generic-pipeline matching

### Requirement: Match records forwarded from child to ledger
When a child actor's `SearchCompleted` reply includes a `MatchRecord`,
`SearchActor` SHALL forward that record to `MatchLedgerActor`. Child actors
SHALL NOT hold or use a reference to `MatchLedgerActor`.

#### Scenario: Match record forwarded to ledger
- **WHEN** `SearchActor` receives a `SearchCompleted` message with a non-null
  `MatchRecord`
- **THEN** `SearchActor` SHALL tell `MatchLedgerActor` to record that match
  result

#### Scenario: No record to forward
- **WHEN** `SearchActor` receives a `SearchCompleted` message with a null
  `MatchRecord`
- **THEN** `SearchActor` SHALL NOT send anything to `MatchLedgerActor` for
  that request

### Requirement: Child actor lifecycle
`SearchActor` SHALL create `TvSearchActor`, `MovieSearchActor`, and
`TextSearchActor` as long-lived children in its `PreStart`, one instance of
each for the lifetime of the parent. Each child SHALL be supervised with
Akka's default supervision strategy, such that an unhandled exception in one
child restarts only that child and does not affect the other children or
the parent's cache and dependency-resolution state.

#### Scenario: Children created once at parent startup
- **WHEN** `SearchActor` starts
- **THEN** it SHALL create exactly one `TvSearchActor`, one
  `MovieSearchActor`, and one `TextSearchActor` as children, independent of
  whether `RuleSetRegistryActor`/`MatchLedgerActor` resolution has completed

#### Scenario: Child failure isolated from siblings and parent
- **WHEN** `TvSearchActor` throws an unhandled exception while processing an
  `ExecuteTvSearch` command
- **THEN** `TvSearchActor` SHALL be restarted per the default supervision
  strategy, and `MovieSearchActor`, `TextSearchActor`, and the parent's
  cache and dependency-resolution state SHALL remain unaffected

#### Scenario: Children are not created per-request
- **WHEN** `SearchActor` routes multiple concurrent `TvSearchRequest`s
- **THEN** all of them SHALL be dispatched to the same single
  `TvSearchActor` instance, not to newly created child actors per request

### Requirement: External API surface unchanged
Callers outside `FunkArr.Search` (indexer endpoints, tests) SHALL continue
to interact with `SearchActor` using the same external message types
(`SearchActor.TvSearchRequest`, `SearchActor.MovieSearchRequest`,
`SearchActor.TextSearchRequest`, `SearchActor.SearchResponse`) and the same
`Ask` pattern as before this change. Internal message types
(`ExecuteTvSearch`, `ExecuteMovieSearch`, `ExecuteTextSearch`,
`SearchCompleted`) SHALL NOT be exposed outside `FunkArr.Search`.

#### Scenario: Endpoint asks SearchActor unchanged
- **WHEN** an indexer endpoint asks `SearchActor` with a `TvSearchRequest`
- **THEN** it SHALL receive a `SearchActor.SearchResponse` exactly as it did
  before this change, with no awareness of `TvSearchActor` or any internal
  message type

#### Scenario: Internal messages inaccessible externally
- **WHEN** code outside `FunkArr.Search` attempts to reference
  `ExecuteTvSearch`, `ExecuteMovieSearch`, `ExecuteTextSearch`, or
  `SearchCompleted`
- **THEN** these types SHALL NOT be accessible (private/internal to the
  routing implementation)
