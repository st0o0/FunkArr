## Purpose

Akka.NET persistent actor that tracks match quality metrics (match/miss/skip outcomes per topic), providing statistics and recent-match queries. Child of RuleSetCoordinator, replaces the former MatchLedgerActor with the same query API.

## Requirements

### Requirement: MatchQualityWorker event-sourced persistence
`MatchQualityWorker` SHALL be a `ReceivePersistentActor` child of `RuleSetCoordinator` with `PersistenceId: "match-quality"`. It SHALL persist `MatchRecorded` and `MatchesExpired` events with snapshots every 500 events.

#### Scenario: Match records survive restart
- **WHEN** `MatchQualityWorker` restarts after a crash
- **THEN** it SHALL recover match records from the latest snapshot + replayed events

### Requirement: Same query API as MatchLedgerActor
`MatchQualityWorker` SHALL respond to `RecordMatchResult`, `GetRecentMatches`, `GetTopicStats`, `GetAllTopicStats`, and `GetUnmatchedItems` with the same message types and behavior as the replaced `MatchLedgerActor`.

#### Scenario: API compatibility
- **WHEN** `GetAllTopicStats` is received
- **THEN** `MatchQualityWorker` SHALL reply with `TopicStatsResponse` containing per-topic statistics sorted by match rate ascending
