## MODIFIED Requirements

### Requirement: TvSearchWorker is a sharded entity

The TvSearchWorker SHALL be a sharded entity using SearchId (Guid) as the shard key. Each search request creates a new worker instance that processes the search and responds. The worker SHALL be passivated automatically via `PassivateIdleEntityAfter` on the shard configuration — no manual Passivate calls.

#### Scenario: Worker creation and auto-passivation

- **WHEN** the TvSearch ShardRegion receives a message with a new SearchId
- **THEN** a new TvSearchWorker instance SHALL be created, process the search, respond, and be passivated automatically after 30 seconds of idle time

### Requirement: TvSearchWorker orchestrates search pipeline

The TvSearchWorker SHALL use Become-based phase transitions to process the search pipeline. The worker SHALL capture the original Sender as `ReplyTo` in state when the TvSearch command arrives. All replies SHALL use `ReplyTo.Tell(...)` instead of `Sender.Tell(...)`. All `PipeTo` calls SHALL omit the Sender parameter.

#### Scenario: Successful TV search with query text

- **WHEN** the worker receives a TvSearch with Query="Tatort"
- **THEN** it SHALL capture ReplyTo from Sender, Become the querying phase, build a MediathekQuery with topic-based search and duration minimum of 300 seconds, Ask the MediathekViewWebManager, then transition through RuleSet resolution and scoring phases, and Tell ReplyTo with a SearchCompleted containing scored items

#### Scenario: ID-only TV search

- **WHEN** the worker receives a TvSearch with Query=null and TvdbId=83214
- **THEN** it SHALL capture ReplyTo, Become the resolving phase, Ask the RuleSetResolver with ResolveRuleSet(null, TvdbId: 83214), use the resolved topic to query Mediathek, then proceed through scoring

#### Scenario: ID-only search with no matching ruleset

- **WHEN** the worker receives a TvSearch with Query=null and TvdbId=99999 and no ruleset maps that ID
- **THEN** the worker SHALL receive a RuleSetFailed containing a RuleSetNotFoundException and Tell ReplyTo with SearchCompleted(SearchId, Items: [], Total: 0)

#### Scenario: Become-based phase transitions

- **WHEN** the worker transitions between pipeline phases
- **THEN** it SHALL use Become to switch handler sets so that each phase only handles messages relevant to that phase

#### Scenario: ScoredResults stored in state

- **WHEN** scoring completes and enrichment is needed
- **THEN** the worker SHALL store ScoredResults in state (not as a separate mutable field) before transitioning to the enriching phase
