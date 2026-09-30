## MODIFIED Requirements

### Requirement: MovieSearchWorker is a sharded entity

The MovieSearchWorker SHALL be a sharded entity using SearchId (Guid) as the shard key. Each search request creates a new worker instance that processes the search and responds. The worker SHALL be passivated automatically via `PassivateIdleEntityAfter` on the shard configuration — no manual Passivate calls.

#### Scenario: Worker creation and auto-passivation

- **WHEN** the MovieSearch ShardRegion receives a message with a new SearchId
- **THEN** a new MovieSearchWorker instance SHALL be created, process the search, respond, and be passivated automatically after 30 seconds of idle time

### Requirement: MovieSearchWorker orchestrates search pipeline

The MovieSearchWorker SHALL use Become-based phase transitions to process the search pipeline. The worker SHALL capture the original Sender as `ReplyTo` in state when the MovieSearch command arrives. All replies SHALL use `ReplyTo.Tell(...)` instead of `Sender.Tell(...)`. All `PipeTo` calls SHALL omit the Sender parameter.

#### Scenario: Successful movie search with query text

- **WHEN** the worker receives a MovieSearch with Query="Das Boot"
- **THEN** it SHALL capture ReplyTo from Sender, Become the querying phase, build a MediathekQuery with title+topic search and duration minimum of 3600 seconds, Ask the MediathekViewWebManager, then transition through RuleSet resolution and scoring phases, and Tell ReplyTo with a SearchCompleted containing scored items

#### Scenario: ImdbId-only movie search

- **WHEN** the worker receives a MovieSearch with Query=null and ImdbId="tt0806910"
- **THEN** it SHALL capture ReplyTo, Become the resolving phase, Ask the RuleSetResolver with ResolveRuleSet(null, ImdbId: "tt0806910"), use the resolved topic to query Mediathek

#### Scenario: ID-only search with no matching ruleset

- **WHEN** the worker receives a MovieSearch with Query=null and ImdbId="tt9999999" and no ruleset maps that ID
- **THEN** the worker SHALL receive a RuleSetFailed containing a RuleSetNotFoundException and Tell ReplyTo with SearchCompleted(SearchId, Items: [], Total: 0)

#### Scenario: Become-based phase transitions

- **WHEN** the worker transitions between pipeline phases
- **THEN** it SHALL use Become to switch handler sets so that each phase only handles messages relevant to that phase

#### Scenario: ScoredResults stored in state

- **WHEN** scoring completes and enrichment is needed
- **THEN** the worker SHALL store ScoredResults in state (not as a separate mutable field) before transitioning to the enriching phase
