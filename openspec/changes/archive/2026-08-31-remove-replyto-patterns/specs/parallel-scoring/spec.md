## MODIFIED Requirements

### Requirement: MatchMagicManager routes scoring to Router Pool
The MatchMagicManager SHALL own a SmallestMailboxPool of MatchMagicActors and route scoring requests to the pool by looking up the MatchingConfig and forwarding the sender identity via `Tell(msg, Sender)`.

#### Scenario: Route scoring request
- **WHEN** MatchMagicManager receives ScoreItems with ruleSetId "die-anstalt" and the config exists
- **THEN** it sends ExecuteScoring(config, items) to the Router Pool with the original Sender preserved

#### Scenario: Unknown ruleSetId
- **WHEN** MatchMagicManager receives ScoreItems with a ruleSetId not in the dictionary
- **THEN** it replies to the sender with ScoreCompleted containing all items scored at 0.0 with matched=false

#### Scenario: Pool size is configurable
- **WHEN** the system starts with pool size configured to N
- **THEN** the Router Pool contains N MatchMagicActor instances

### Requirement: MatchMagicActor is stateless
The MatchMagicActor SHALL hold no state. It receives ExecuteScoring messages containing config and items, and replies to `Sender` (the original caller, propagated by MatchMagicManager).

#### Scenario: Stateless processing
- **WHEN** MatchMagicActor receives ExecuteScoring with config and items
- **THEN** it evaluates all items against the config's rules and sends ScoreCompleted to Sender

#### Scenario: Concurrent scoring
- **WHEN** two ScoreItems arrive at MatchMagicManager simultaneously and pool size >= 2
- **THEN** both are processed concurrently by different MatchMagicActor instances
