## MODIFIED Requirements

### Requirement: MovieSearchWorker orchestrates search pipeline

The MovieSearchWorker SHALL chain MediathekViewWeb query and MatchMagic scoring using PipeTo, without blocking the actor thread. When only IDs are provided (no query text), the worker SHALL resolve the topic from the RuleSet domain first.

#### Scenario: Successful movie search with query text

- **WHEN** the worker receives a MovieSearchCommand with Query="Das Boot"
- **THEN** it SHALL build a MediathekQuery with title+topic search and duration minimum of 3600 seconds, Ask the MediathekViewWebManager, then Ask the MatchMagicManager for scoring, and Tell the Sender with a SearchCompleted containing scored items

#### Scenario: ImdbId-only movie search

- **WHEN** the worker receives a MovieSearchCommand with Query=null and ImdbId="tt0806910"
- **THEN** it SHALL Ask the RuleSetResolver with ResolveRuleSet(null, ImdbId: "tt0806910"), use the resolved topic to build a MediathekQuery with title+topic search and duration minimum 3600, then proceed with standard scoring

#### Scenario: TmdbId-only movie search

- **WHEN** the worker receives a MovieSearchCommand with Query=null and TmdbId=550
- **THEN** it SHALL Ask the RuleSetResolver with ResolveRuleSet(null, TmdbId: 550), use the resolved topic to query MVW

#### Scenario: ID-only search with no matching ruleset

- **WHEN** the worker receives a MovieSearchCommand with Query=null and ImdbId="tt9999999" and no ruleset maps that ID
- **THEN** the worker SHALL Tell the Sender with SearchCompleted(SearchId, Items: [], Total: 0)

#### Scenario: No query and no IDs

- **WHEN** the worker receives a MovieSearchCommand with Query=null and ImdbId=null and TmdbId=null
- **THEN** the worker SHALL Tell the Sender with SearchFailed(SearchId, "Movie search requires a query or media ID")

#### Scenario: Text search carries IDs through to results

- **WHEN** the worker receives a MovieSearchCommand with Query="Das Boot" and ImdbId="tt1234567"
- **THEN** the worker SHALL use the text-based flow and set ImdbId="tt1234567" on each SearchResultItem

#### Scenario: MediathekViewWeb query fails

- **WHEN** the MediathekViewWebManager Ask times out or returns an error
- **THEN** the worker SHALL Tell the Sender with a SearchFailed and passivate

#### Scenario: MatchMagic scoring fails

- **WHEN** the MatchMagicManager Ask times out or returns an error
- **THEN** the worker SHALL Tell the Sender with a SearchFailed and passivate
