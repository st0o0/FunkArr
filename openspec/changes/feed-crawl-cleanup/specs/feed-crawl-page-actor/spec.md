## MODIFIED Requirements

### Requirement: FeedCrawlPageActor processes one page end-to-end

FeedCrawlPageActor SHALL be a non-persistent ReceiveActor pool routee that receives a `ProcessPage` message and independently fetches one MVW page, filters by window, groups by ruleset, evaluates via Search domain, applies gating logic (private static methods), and replies with `ProcessPageResponse`.

#### Scenario: Successful page processing

- **WHEN** FeedCrawlPageActor receives ProcessPage(offset=0, window, pageSize=1000, lookup, minConfidence=0.7)
- **THEN** it SHALL Ask MediathekManager for the page
- **AND** filter items by the crawl window bounds
- **AND** deduplicate by stable ID (private static `StableIdFor`)
- **AND** group filtered items by ruleset via TopicTargets dictionary lookup
- **AND** Ask TvEvaluationRegion or MovieEvaluationRegion per group
- **AND** apply private static `Evaluate` per group
- **AND** reply ProcessPageCompleted with accepted entries and stats

#### Scenario: No items in window

- **WHEN** MVW returns items but none fall within the crawl window
- **THEN** ProcessPageCompleted SHALL have empty accepted entries, newItems=0, and reachedEnd based on page size

#### Scenario: No ruleset matches

- **WHEN** all items in the window have topics that match no registered ruleset
- **THEN** ProcessPageCompleted SHALL have empty accepted entries and unmatched equal to the filtered item count

### Requirement: FetchCompleted PipeTo wrapper carries extracted data

The `FetchCompleted` private PipeTo wrapper SHALL extract the fields the actor needs from `QueryMediathekCompleted` instead of wrapping the entire response message. `ProcessPage` stays on the wrapper because the actor needs the full request context for evaluation routing.

#### Scenario: FetchCompleted shape

- **WHEN** the MediathekManager Ask completes successfully
- **THEN** the PipeTo success mapper SHALL extract `Items` and `ListTimestamp` from `QueryMediathekCompleted`
- **AND** wrap them as `FetchCompleted(MediathekItem[] Items, long ListTimestamp, ProcessPage Request)`
- **AND** NOT wrap the entire `QueryMediathekCompleted` response

### Requirement: EvaluateSeries carries flat properties

`EvaluateSeries` SHALL carry flat properties instead of embedding a `RuleSetResolved` response record. The message SHALL have: SearchId (Guid), Source (SearchSource), Items (MediathekItem[]), TvdbId (int?), ImdbId (string?), Season (int?), RuleSetId (string?), MediaName (string?), Enrichment (EnrichmentConfig?).

#### Scenario: EvaluateSeries without RuleSetResolved

- **WHEN** `EvaluateSeries` is constructed
- **THEN** it SHALL NOT have a `RuleSet` property of type `RuleSetResolved?`
- **AND** it SHALL have `RuleSetId (string?)`, `MediaName (string?)`, and `Enrichment (EnrichmentConfig?)` as direct properties

#### Scenario: Feed domain constructs EvaluateSeries with flat fields

- **WHEN** FeedCrawlPageActor constructs an EvaluateSeries message
- **THEN** it SHALL pass `RuleSetId`, `MediaName`, and `Enrichment` as separate parameters (not wrapped in RuleSetResolved)

#### Scenario: Search evaluation state reads flat fields

- **WHEN** TvEvaluationState.Init receives an EvaluateSeries command
- **THEN** it SHALL read `cmd.RuleSetId`, `cmd.MediaName`, and `cmd.Enrichment` directly
- **AND** NOT destructure a `cmd.RuleSet` property

### Requirement: EvaluateMovies carries flat properties

`EvaluateMovies` SHALL carry flat properties instead of embedding a `RuleSetResolved` response record. The message SHALL have: SearchId (Guid), Source (SearchSource), Items (MediathekItem[]), ImdbId (string?), TmdbId (int?), RuleSetId (string?), MediaName (string?), Enrichment (EnrichmentConfig?).

#### Scenario: EvaluateMovies without RuleSetResolved

- **WHEN** `EvaluateMovies` is constructed
- **THEN** it SHALL NOT have a `RuleSet` property of type `RuleSetResolved?`
- **AND** it SHALL have `RuleSetId (string?)`, `MediaName (string?)`, and `Enrichment (EnrichmentConfig?)` as direct properties

#### Scenario: Feed domain constructs EvaluateMovies with flat fields

- **WHEN** FeedCrawlPageActor constructs an EvaluateMovies message
- **THEN** it SHALL pass `RuleSetId`, `MediaName`, and `Enrichment` as separate parameters (not wrapped in RuleSetResolved)

#### Scenario: Search evaluation state reads flat fields

- **WHEN** MovieEvaluationState.Init receives an EvaluateMovies command
- **THEN** it SHALL read `cmd.RuleSetId`, `cmd.MediaName`, and `cmd.Enrichment` directly
- **AND** NOT destructure a `cmd.RuleSet` property
