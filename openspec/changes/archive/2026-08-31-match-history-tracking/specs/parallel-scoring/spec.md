## MODIFIED Requirements

### Requirement: MatchMagicManager routes scoring to Router Pool

The MatchMagicManager SHALL own a SmallestMailboxPool of MatchMagicActors and route scoring requests to the pool. When routing, it SHALL resolve the MatchHistory ShardRegion and include it in the ExecuteScoring message along with RequestId and Origin from the ScoreItems command.

#### Scenario: Route scoring request

- **WHEN** MatchMagicManager receives ScoreItems with RequestId=abc, RuleSetId="die-anstalt", Origin=ScoringOrigin("sonarr", "Die Anstalt") and the config exists
- **THEN** it sends ExecuteScoring(config, items, RequestId=abc, Origin, HistoryRef) to the Router Pool with the original Sender preserved

#### Scenario: Unknown ruleSetId

- **WHEN** MatchMagicManager receives ScoreItems with a ruleSetId not in the dictionary
- **THEN** it replies to the sender with ScoreCompleted(RequestId, defaults) containing all items scored at 0.0 with matched=false

#### Scenario: Pool size is configurable

- **WHEN** the system starts with pool size configured to N
- **THEN** the Router Pool contains N MatchMagicActor instances

### Requirement: ExecuteScoring message carries trace context

The internal ExecuteScoring message (MatchMagicManager → MatchMagicActor) SHALL carry RequestId (Guid), Origin (ScoringOrigin), and HistoryRef (IActorRef) in addition to the existing Config and Items.

#### Scenario: ExecuteScoring fields

- **WHEN** MatchMagicManager constructs an ExecuteScoring message
- **THEN** it SHALL contain: Config (MatchingConfig), Items (ScoreCandidate[]), RequestId (Guid), Origin (ScoringOrigin), HistoryRef (IActorRef)

#### Scenario: HistoryRef resolution

- **WHEN** MatchMagicManager starts
- **THEN** it SHALL resolve the MatchHistory ShardRegion via Context.GetActor and cache the reference for use in all ExecuteScoring messages
