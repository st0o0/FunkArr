## Purpose

In-memory match result recording with per-item trace information, aggregate statistics computation per topic, and configurable retention via a bounded CircularQueue managed by a MatchLedgerActor.

## Requirements

### Requirement: Match ledger actor
The system SHALL provide a MatchLedgerActor registered via Akka.Hosting that records match results in a bounded in-memory collection (CircularQueue) and responds to queries for match data.

#### Scenario: Actor registration
- **WHEN** the application starts
- **THEN** the MatchLedgerActor SHALL be registered in the ActorSystem and resolvable via IActorRegistry

#### Scenario: Memory bounds
- **WHEN** the ledger reaches its configured capacity (default 10,000 entries)
- **THEN** the oldest entries SHALL be evicted automatically via CircularQueue

### Requirement: Match record structure
Each match record SHALL contain: a unique record ID, timestamp, search parameters (topic, tvdbId, season, episode), the total number of Mediathek results evaluated, and three categorized lists: matched items, filtered items, and unmatched items -- each with per-item trace information.

#### Scenario: Complete match record
- **WHEN** a search for "Tatort" S01E05 evaluates 47 Mediathek results
- **THEN** the ledger SHALL store a record with searchTopic="Tatort", tvdbId=83214, season=1, episode=5, totalResults=47, and categorized item lists

#### Scenario: Match record includes search timing
- **WHEN** a match record is created
- **THEN** it SHALL include the UTC timestamp of when the search occurred

### Requirement: Matched item trace
Each matched item in the ledger SHALL record which rule produced the match (rule index, strategy, confidence) and the resulting TVDB episode info.

#### Scenario: Matched item with rule trace
- **WHEN** item "Tatort - Der letzte Schrei" matches via rule #1 (seasonEpisode strategy, confidence 0.95)
- **THEN** the trace SHALL include ruleIndex=0, strategy=SeasonAndEpisodeNumber, confidence=0.95, and the matched TVDB season/episode

### Requirement: Filtered item trace
Each filtered item SHALL record which filter caused the exclusion, including the filter field, operator, expected value, and actual value.

#### Scenario: Filtered by accessibility keyword
- **WHEN** item "Tatort - Der letzte Schrei (AD)" is filtered by a NOT filter matching "Audiodeskription"
- **THEN** the trace SHALL include filterField="title", filterOp="contains", filterValue="Audiodeskription", reason="not-filter"

#### Scenario: Filtered by duration
- **WHEN** item "Making Of" with duration 300s is filtered by duration >= 30min
- **THEN** the trace SHALL include filterField="duration", filterOp="greaterThan", filterValue="30", actualValue="5"

### Requirement: Unmatched item trace
Each unmatched item SHALL record the evaluation path -- which rules were tried and why each failed.

#### Scenario: Rule failed due to regex miss
- **WHEN** item "Tatort - Borowski" fails rule #1 because seasonRegex did not match the title
- **THEN** the trace SHALL include ruleIndex=0, failReason="regex-no-match", field="title", pattern=the seasonRegex

#### Scenario: Rule failed due to no TVDB match
- **WHEN** item "Tatort - Borowski" passes filters and regex extraction produces S01E99 but TVDB has no such episode
- **THEN** the trace SHALL include ruleIndex=0, failReason="tvdb-no-match", extractedSeason=1, extractedEpisode=99

#### Scenario: All rules exhausted
- **WHEN** an item fails all rules in the ruleset
- **THEN** the trace SHALL contain failure entries for every rule attempted

### Requirement: Record match events from SearchActor
The SearchActor SHALL tell match event messages to the MatchLedgerActor after completing each search, containing the full match result with traces for all evaluated items.

#### Scenario: Search produces match event
- **WHEN** SearchActor completes a TV search for tvdbId=83214 S01E05
- **THEN** it SHALL send a RecordMatchResult message to the MatchLedgerActor with all match traces

#### Scenario: Search with no ruleset still records
- **WHEN** SearchActor falls back to the generic MatchingPipeline (no ruleset found)
- **THEN** it SHALL still send a RecordMatchResult to the ledger with source="generic-pipeline"

### Requirement: Aggregate statistics computation
The MatchLedgerActor SHALL compute aggregate statistics per topic on demand: total searches, total items evaluated, match rate (matched / total), and per-rule hit counts.

#### Scenario: Topic stats query
- **WHEN** a GetTopicStats query is received for topic "Tatort"
- **THEN** the actor SHALL return: searchCount, totalItemsEvaluated, matchedCount, filteredCount, unmatchedCount, matchRate, and a per-rule breakdown of hit counts

#### Scenario: All topics stats
- **WHEN** a GetAllTopicStats query is received
- **THEN** the actor SHALL return aggregated stats for every topic that has ledger entries, sorted by match rate ascending (worst first)

### Requirement: Configurable retention
The ledger capacity SHALL be configurable via `FunkArr__MatchLedgerCapacity` with a default of 10,000 entries.

#### Scenario: Custom capacity
- **WHEN** `FunkArr__MatchLedgerCapacity` is set to 5000
- **THEN** the ledger SHALL evict entries when count exceeds 5000

#### Scenario: Default capacity
- **WHEN** no capacity setting is configured
- **THEN** the ledger SHALL use 10,000 as the default capacity
