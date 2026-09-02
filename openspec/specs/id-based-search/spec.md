# ID-Based Search

## Purpose

ID-based search resolution enabling the RuleSetResolver, TvSearchWorker, and MovieSearchWorker to resolve media by external IDs (tvdbId, imdbId, tmdbId) when no query text is provided.

## Requirements

### Requirement: RuleSetResolver supports ID-based resolution

The RuleSetResolver SHALL maintain an ID index alongside its topic/alias index. When a `ResolveRuleSet` query includes tvdbId, imdbId, or tmdbId, the resolver SHALL attempt topic/alias lookup first, then fall back to ID-based lookup.

#### Scenario: Resolve by tvdbId

- **WHEN** `ResolveRuleSet(TopicOrAlias: null, TvdbId: 83214)` is received and ruleset "tatort" is registered with TvdbId 83214
- **THEN** the resolver SHALL respond with `RuleSetResolved("tatort", "Tatort")`

#### Scenario: Resolve by imdbId

- **WHEN** `ResolveRuleSet(TopicOrAlias: null, ImdbId: "tt0806910")` is received and ruleset "tatort" is registered with ImdbId "tt0806910"
- **THEN** the resolver SHALL respond with `RuleSetResolved("tatort", "Tatort")`

#### Scenario: Resolve by tmdbId

- **WHEN** `ResolveRuleSet(TopicOrAlias: null, TmdbId: 2116)` is received and ruleset "tatort" is registered with TmdbId 2116
- **THEN** the resolver SHALL respond with `RuleSetResolved("tatort", "Tatort")`

#### Scenario: Topic lookup takes precedence over ID lookup

- **WHEN** `ResolveRuleSet(TopicOrAlias: "Tatort", TvdbId: 99999)` is received and topic "Tatort" resolves to "tatort"
- **THEN** the resolver SHALL respond with `RuleSetResolved("tatort", "Tatort")` using the topic match, ignoring the non-matching TvdbId

#### Scenario: ID-only resolve with no matching ruleset

- **WHEN** `ResolveRuleSet(TopicOrAlias: null, TvdbId: 99999)` is received and no ruleset maps TvdbId 99999
- **THEN** the resolver SHALL respond with `RuleSetNotFound`

#### Scenario: Multiple ID types provided

- **WHEN** `ResolveRuleSet(TopicOrAlias: null, TvdbId: 83214, ImdbId: "tt0806910")` is received
- **THEN** the resolver SHALL resolve using the first matching ID (tvdbId checked first, then imdbId, then tmdbId)

### Requirement: ID index updated on registration and deregistration

The resolver SHALL update the ID index when rulesets are registered or deregistered.

#### Scenario: Registration with media IDs

- **WHEN** `RegisterRuleSet("tatort", "Tatort", ["Tatort - Munster"], TvdbId: 83214, ImdbId: "tt0806910", TmdbId: 2116)` is received
- **THEN** the ID index SHALL contain entries mapping all three IDs to ruleSetId "tatort" with topic "Tatort"

#### Scenario: Registration without media IDs

- **WHEN** `RegisterRuleSet("custom-show", "Custom Show", [], TvdbId: null, ImdbId: null, TmdbId: null)` is received
- **THEN** the ID index SHALL NOT contain entries for "custom-show" and topic/alias resolution SHALL still work

#### Scenario: Re-registration clears previous ID mappings

- **WHEN** a ruleset "tatort" was registered with TvdbId 83214, then re-registered with TvdbId 99999
- **THEN** TvdbId 83214 SHALL no longer resolve, and TvdbId 99999 SHALL resolve to "tatort"

#### Scenario: Deregistration removes ID mappings

- **WHEN** `DeregisterRuleSet("tatort")` is received
- **THEN** all ID index entries for "tatort" SHALL be removed

### Requirement: TvSearchWorker handles ID-only searches

The TvSearchWorker SHALL support search requests where only IDs are provided (no query text). It SHALL resolve the topic from the RuleSet domain first, then query MediathekViewWeb with the resolved topic.

#### Scenario: TvdbId-only search

- **WHEN** a `TvSearchCommand(SearchId, Query: null, Season: null, Episode: null, TvdbId: 83214, ImdbId: null)` is received
- **THEN** the worker SHALL first Ask `RuleSetResolver` with `ResolveRuleSet(null, TvdbId: 83214)`, receive `RuleSetResolved("tatort", "Tatort")`, then query MVW with topic "Tatort"

#### Scenario: ID-only search with season and episode

- **WHEN** a `TvSearchCommand(SearchId, Query: null, Season: 1, Episode: 3, TvdbId: 83214, ImdbId: null)` is received
- **THEN** the worker SHALL resolve the topic via ID, then query MVW with topic "Tatort" and pass season/episode for result context

#### Scenario: ID-only search with no matching ruleset

- **WHEN** a `TvSearchCommand(SearchId, Query: null, TvdbId: 99999)` is received and no ruleset maps TvdbId 99999
- **THEN** the worker SHALL respond with `SearchCompleted(SearchId, Items: [], Total: 0)`

#### Scenario: Text search with IDs passes IDs through

- **WHEN** a `TvSearchCommand(SearchId, Query: "Tatort", TvdbId: 83214)` is received
- **THEN** the worker SHALL use the existing text-based flow and carry TvdbId through to SearchResultItem

### Requirement: MovieSearchWorker handles ID-only searches

The MovieSearchWorker SHALL support search requests where only IDs are provided (no query text). It SHALL resolve the topic from the RuleSet domain first, then query MediathekViewWeb with the resolved topic.

#### Scenario: ImdbId-only search

- **WHEN** a `MovieSearchCommand(SearchId, Query: null, ImdbId: "tt0806910", TmdbId: null)` is received
- **THEN** the worker SHALL first Ask `RuleSetResolver` with `ResolveRuleSet(null, ImdbId: "tt0806910")`, receive `RuleSetResolved("tatort", "Tatort")`, then query MVW with topic "Tatort" using duration minimum 3600

#### Scenario: TmdbId-only search

- **WHEN** a `MovieSearchCommand(SearchId, Query: null, ImdbId: null, TmdbId: 550)` is received
- **THEN** the worker SHALL resolve the topic via TmdbId, then query MVW with the resolved topic

#### Scenario: ID-only search with no matching ruleset

- **WHEN** a `MovieSearchCommand(SearchId, Query: null, ImdbId: "tt9999999")` is received and no ruleset maps that ImdbId
- **THEN** the worker SHALL respond with `SearchCompleted(SearchId, Items: [], Total: 0)`

#### Scenario: No query and no IDs

- **WHEN** a `MovieSearchCommand(SearchId, Query: null, ImdbId: null, TmdbId: null)` is received
- **THEN** the worker SHALL respond with `SearchFailed(SearchId, "Movie search requires a query or media ID")`
