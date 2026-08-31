## Requirements

### Requirement: MatchMagicManager stores MatchingConfig
The MatchMagicManager (Singleton) SHALL maintain a `Dictionary<string, MatchingConfig>` keyed by ruleSetId, updated when MatchingConfig messages arrive from RuleSetWorkers.

#### Scenario: Receive initial config
- **WHEN** MatchMagicManager receives a MatchingConfig with ruleSetId "die-anstalt"
- **THEN** it stores the config in its dictionary under key "die-anstalt"

#### Scenario: Config update replaces previous
- **WHEN** MatchMagicManager receives a new MatchingConfig for an already-stored ruleSetId
- **THEN** the previous config is replaced with the new one

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

### Requirement: MatchMagicActor evaluates filters
The MatchMagicActor SHALL evaluate FilterSpec conditions against each candidate item. FilterCondition evaluation MUST support all FilterOp values and resolve FilterField to the corresponding item property.

#### Scenario: Duration greater than filter
- **WHEN** a FilterCondition is `(Duration, GreaterThan, "40")` and the item has duration 50 minutes
- **THEN** the condition evaluates to true

#### Scenario: Nested filter group
- **WHEN** a FilterSpec has `All: [Condition(Duration, GreaterThan, "40"), Group(Any: [Condition(Channel, Eq, "ZDF"), Condition(Channel, Eq, "ARD")])]` and item has duration 50, channel "ZDF"
- **THEN** the filter evaluates to true

#### Scenario: Not filter excludes
- **WHEN** a FilterSpec has `Not: [Condition(Title, Contains, "Trailer")]` and item title contains "Trailer"
- **THEN** the filter evaluates to false

### Requirement: MatchMagicActor identifies episodes via RegexCapture
The MatchMagicActor SHALL extract season and/or episode numbers from item titles using regex patterns when the strategy is RegexCapture.

#### Scenario: Season and episode extraction
- **WHEN** a MatchingRule has RegexCapture with SeasonPattern and EpisodePattern, and the title matches both
- **THEN** the result contains Season and Episode values from captured groups

#### Scenario: Absolute episode only
- **WHEN** a MatchingRule has RegexCapture with only EpisodePattern (SeasonPattern is null), and the title matches
- **THEN** the result contains only Episode, Season is null

#### Scenario: No match
- **WHEN** the title does not match the episode regex
- **THEN** the rule does not match and the next rule is tried

### Requirement: MatchMagicActor identifies episodes via TitleConstruction
The MatchMagicActor SHALL construct a title from TitleParts and compare it to the item title using the specified TitleMatchMode.

#### Scenario: Static and regex parts combined
- **WHEN** TitleParts are [Static("Folge "), Regex("\\w+\\s+\\((\\d+)\\)$", Field=Title)] and item title is "Waldkraiburg (42)"
- **THEN** the constructed title is "Folge 42"

#### Scenario: Exact match mode
- **WHEN** MatchMode is Exact and constructed title equals item title (case-insensitive)
- **THEN** the rule matches

#### Scenario: Contains match mode
- **WHEN** MatchMode is Contains and item title contains the constructed title (case-insensitive, umlaut-normalized)
- **THEN** the rule matches

#### Scenario: Title part extraction fails
- **WHEN** a regex TitlePart does not match the item's field value
- **THEN** title construction fails and the rule does not match

### Requirement: MatchMagicActor identifies episodes via AirdateExtraction
The MatchMagicActor SHALL extract German dates from item titles and use the formatted date (yyyy-MM-dd) as the episode title.

#### Scenario: Numeric German date
- **WHEN** item title contains "15.01.2025"
- **THEN** the identification title is "2025-01-15"

#### Scenario: Written German month
- **WHEN** item title contains "28. Januar 2025"
- **THEN** the identification title is "2025-01-28"

#### Scenario: No date found
- **WHEN** item title contains no recognizable German date
- **THEN** the rule does not match

### Requirement: MatchMagicActor applies priority ordering
The MatchMagicActor SHALL evaluate rules in ascending priority order. The first rule that matches an item wins; no further rules are evaluated for that item.

#### Scenario: Lower priority rule wins
- **WHEN** an item matches rules with priority 0 and priority 1
- **THEN** the priority 0 rule's result is used

#### Scenario: Fallback to higher priority
- **WHEN** an item does not match the priority 0 rule but matches priority 1
- **THEN** the priority 1 rule's result is used

### Requirement: ScoreItems requires ruleSetId
The ScoreItems message SHALL require a non-null ruleSetId. The previous fallback behavior (null ruleSetId → pick first available) is removed.

#### Scenario: ScoreItems with ruleSetId
- **WHEN** SearchWorker sends ScoreItems with ruleSetId "die-anstalt"
- **THEN** MatchMagicManager looks up config for "die-anstalt"

### Requirement: SearchWorker resolves ruleSetId via RuleSetResolver
TvSearchWorker and MovieSearchWorker SHALL query the RuleSetResolver for the ruleSetId before sending ScoreItems to MatchMagicManager.

#### Scenario: Successful resolution and scoring
- **WHEN** SearchWorker receives a search command, it asks RuleSetResolver for the ruleSetId
- **THEN** after receiving RuleSetResolved, it uses the ruleSetId in ScoreItems

#### Scenario: Resolution fails
- **WHEN** RuleSetResolver responds with RuleSetNotFound
- **THEN** SearchWorker returns search results with all items scored at 0.0
