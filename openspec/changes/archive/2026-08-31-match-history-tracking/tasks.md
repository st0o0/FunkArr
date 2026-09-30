## 1. Message Redesign

- [x] 1.1 Add ScoringOrigin record to FunkArr.Messages/Scoring/
- [x] 1.2 Extend ScoreCandidate with Description (string?) and Timestamp (long)
- [x] 1.3 Extend ScoreItems with RequestId (Guid) and Origin (ScoringOrigin), rename Items to Candidates
- [x] 1.4 Extend ScoreCompleted with RequestId (Guid)
- [x] 1.5 Extend ExecuteScoring with RequestId, Origin, HistoryRef (IActorRef)
- [x] 1.6 Update TvSearchWorker to populate RequestId, Origin, Description, Timestamp
- [x] 1.7 Update MovieSearchWorker to populate RequestId, Origin, Description, Timestamp
- [x] 1.8 Fix all existing tests for new message signatures (MatchMagicActorTests, MatchMagicManagerTests, TvSearchWorkerTests, MovieSearchWorkerTests)

## 2. Scoring Trace Model

- [x] 2.1 Add RuleOutcome enum to FunkArr.Messages/Scoring/History/
- [x] 2.2 Add FilterNodeTrace record (Field, Op, ExpectedValue, ActualValue, Passed, Skipped, nested Group)
- [x] 2.3 Add FilterGroupTrace record (Operator, Passed, Nodes)
- [x] 2.4 Add IdentificationTrace record (Strategy, Attempted, Detail)
- [x] 2.5 Add TracedIdentification record (Season, Episode, Title)
- [x] 2.6 Add RuleTrace record (RuleId, Priority, Outcome, FilterTrace, IdentificationTrace)
- [x] 2.7 Add ItemTrace record (candidate fields, Matched, Score, MatchedRuleId, Identification, RuleTraces)
- [x] 2.8 Add RecordScoringResult record (RequestId, RuleSetId, Origin, Timestamp, CandidateCount, MatchedCount, ItemTraces)

## 3. Trace Building in MatchMagicActor

- [x] 3.1 Refactor EvaluateFilters to return FilterGroupTrace alongside bool result
- [x] 3.2 Implement short-circuit tracking: mark unevaluated conditions as Skipped with ActualValue=null
- [x] 3.3 Refactor Identify to return IdentificationTrace alongside bool result, capturing strategy name and failure detail
- [x] 3.4 Refactor Handle(ExecuteScoring) to build ItemTrace per candidate and RuleTrace per evaluated rule
- [x] 3.5 Send ScoreCompleted to Sender, then fire-and-forget RecordScoringResult to HistoryRef
- [x] 3.6 Add/update MatchMagicActorTests verifying trace content for matched, unmatched, and short-circuited scenarios

## 4. MatchMagicManager Wiring

- [x] 4.1 Resolve IMatchHistoryService (MatchHistory ShardRegion) in MatchMagicManager constructor
- [x] 4.2 Pass RequestId, Origin, and HistoryRef through ExecuteScoring to pool workers
- [x] 4.3 Include RequestId in ScoreCompleted for the unknown-ruleSetId fallback path
- [x] 4.4 Update MatchMagicManagerTests for new message shapes and HistoryRef forwarding

## 5. Persistence DTOs

- [x] 5.1 Create FunkArr.Persistence/MatchHistory/ directory
- [x] 5.2 Add ScoringRecordedDto with Version, all fields, and [JsonPropertyName] attributes
- [x] 5.3 Add ItemTraceDto with [JsonPropertyName] attributes
- [x] 5.4 Add RuleTraceDto with [JsonPropertyName] attributes
- [x] 5.5 Add FilterGroupTraceDto with [JsonPropertyName] attributes
- [x] 5.6 Add FilterNodeTraceDto with [JsonPropertyName] attributes
- [x] 5.7 Add IdentificationTraceDto and TracedIdentificationDto with [JsonPropertyName] attributes
- [x] 5.8 Add static mapping methods: Message records → DTOs and DTOs → Message records

## 6. JSON Snapshot Tests

- [x] 6.1 Create FunkArr.MatchMagic.Tests/Snapshots/ directory
- [x] 6.2 Create golden file ScoringRecordedDto_v1.json with fully-populated example
- [x] 6.3 Write snapshot test: serialize fully-populated DTO, compare against golden file
- [x] 6.4 Write roundtrip test: deserialize golden file, re-serialize, assert identical output

## 7. MatchHistoryWorker

- [x] 7.1 Add MatchHistoryOptions config record and bind to FunkArr:MatchHistory in appsettings.json
- [x] 7.2 Add IMatchHistoryService marker interface to FunkArr.Core/ActorKeys.cs
- [x] 7.3 Implement MatchHistoryWorker: ReceivePersistentActor with PersistenceId, Recover<ScoringRecordedDto>, Receive<RecordScoringResult>
- [x] 7.4 Implement state management: State record with ImmutableList<ScoringSnapshot>, apply on recover and persist
- [x] 7.5 Implement retention trimming (max count + max age) applied after persist and on recovery
- [x] 7.6 Implement Akka.Persistence snapshot saving every N events (configurable SnapshotInterval)
- [x] 7.7 Implement passivation via Context.SetReceiveTimeout(5 minutes)
- [x] 7.8 Implement QueryScoringHistory handler: paginated summary response from state
- [x] 7.9 Implement QueryScoringDetail handler: lookup by RequestId, return full trace or ScoringDetailNotFound

## 8. Host Registration

- [x] 8.1 Register MatchHistoryWorker shard region in AkkaSetupContainer (WithResolvableActors)
- [x] 8.2 Add shard message extractor for RecordScoringResult and query messages (extract RuleSetId)
- [x] 8.3 Add MatchHistory config section to appsettings.json and appsettings.Development.json

## 9. Query Messages

- [x] 9.1 Add QueryScoringHistory record to FunkArr.Messages/Scoring/History/
- [x] 9.2 Add ScoringSnapshotSummary record
- [x] 9.3 Add ScoringHistoryResult record
- [x] 9.4 Add QueryScoringDetail record
- [x] 9.5 Add ScoringDetailResult record
- [x] 9.6 Add ScoringDetailNotFound record

## 10. MatchHistoryWorker Tests

- [x] 10.1 Test: RecordScoringResult persists and appears in state
- [x] 10.2 Test: QueryScoringHistory returns paginated summaries newest-first
- [x] 10.3 Test: QueryScoringDetail returns full trace for known RequestId
- [x] 10.4 Test: QueryScoringDetail returns ScoringDetailNotFound for unknown RequestId
- [x] 10.5 Test: Retention trimming removes snapshots exceeding MaxSnapshots
- [x] 10.6 Test: Retention trimming removes snapshots exceeding MaxAgeDays
- [x] 10.7 Test: Recovery replays events and applies retention
