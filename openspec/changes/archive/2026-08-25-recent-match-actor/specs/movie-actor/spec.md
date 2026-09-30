## MODIFIED Requirements

### Requirement: Match handler produces full traces
The `MovieActor.HandleMatch` method SHALL produce matched, filtered, and unmatched traces. After replying to the caller, it SHALL Tell `RecentMatchActor.RecordMatch` with the full MatchRecord.

#### Scenario: Match produces traces and records
- **WHEN** MovieActor receives a `Match` message with items
- **THEN** it SHALL evaluate with traces, reply with `MatchedResults`, and Tell `RecentMatchActor.RecordMatch` with the complete MatchRecord including all trace types

#### Scenario: No rules available
- **WHEN** MovieActor receives a `Match` message but has no effective rules
- **THEN** it SHALL NOT tell the RecentMatchActor (no meaningful match data)
