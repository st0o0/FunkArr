## MODIFIED Requirements

### Requirement: TvSearchWorker orchestrates search pipeline

The TvSearchWorker SHALL chain MediathekViewWeb query and MatchMagic scoring using PipeTo, without blocking the actor thread. When only IDs are provided (no query text), the worker SHALL resolve the topic from the RuleSet domain first.

#### Scenario: Successful TV search with query text

- **WHEN** the worker receives a TvSearchCommand with Query="Tatort"
- **THEN** it SHALL build a MediathekQuery with topic-based search and duration minimum of 300 seconds, Ask the MediathekViewWebManager, then Ask the MatchMagicManager for scoring, and Tell the Sender with a SearchCompleted containing scored items

#### Scenario: ID-only TV search

- **WHEN** the worker receives a TvSearchCommand with Query=null and TvdbId=83214
- **THEN** it SHALL Ask the RuleSetResolver with ResolveRuleSet(null, TvdbId: 83214), use the resolved topic to build a MediathekQuery, then proceed with the standard MVW query and scoring flow

#### Scenario: ID-only search with no matching ruleset

- **WHEN** the worker receives a TvSearchCommand with Query=null and TvdbId=99999 and no ruleset maps that ID
- **THEN** the worker SHALL Tell the Sender with SearchCompleted(SearchId, Items: [], Total: 0)

#### Scenario: Text search carries IDs through to results

- **WHEN** the worker receives a TvSearchCommand with Query="Tatort" and TvdbId=83214
- **THEN** the worker SHALL use the text-based flow and set TvdbId=83214 on each SearchResultItem

#### Scenario: MediathekViewWeb query fails

- **WHEN** the MediathekViewWebManager Ask times out or returns an error
- **THEN** the worker SHALL Tell the Sender with a SearchFailed and passivate

#### Scenario: MatchMagic scoring fails

- **WHEN** the MatchMagicManager Ask times out or returns an error
- **THEN** the worker SHALL Tell the Sender with a SearchFailed and passivate
