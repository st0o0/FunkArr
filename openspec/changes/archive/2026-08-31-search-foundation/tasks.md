## 1. Messages

- [x] 1.1 Add IWithSearchId interface to FunkArr.Messages (Guid SearchId, analogous to IWithDownloadId)
- [x] 1.2 Add search command records: TvSearchCommand, MovieSearchCommand (implement IWithSearchId)
- [x] 1.3 Add search response records: SearchCompleted (with SearchResultItem[]), SearchFailed
- [x] 1.4 Add mediathek message records: MediathekQuery, MediathekQueryField, MediathekQueryCompleted, MediathekQueryFailed, MediathekItem (all quality variants)
- [x] 1.5 Add scoring message records: ScoreItems, ScoreCandidate, ScoreCompleted, ScoredItem, LoadRuleSet, UnloadRuleSet

## 2. Core — Shard Routing

- [x] 2.1 Update ShardMessageExtractor to support IWithSearchId alongside IWithDownloadId

## 3. MediathekViewWeb Gateway

- [x] 3.1 Implement MediathekQueryBuilder — fluent API mapping all MediathekViewWeb query features to JSON (Content-Type: text/plain quirk)
- [x] 3.2 Add MediathekQueryBuilder tests — full query, minimal query, duration filters, multiple query fields
- [x] 3.3 Implement MediathekViewWebManager singleton actor — HTTP POST via HttpClient, JSON deserialization to MediathekItem, respond with MediathekQueryCompleted/MediathekQueryFailed
- [x] 3.4 Add stashing-based backpressure to MediathekViewWebManager — configurable max concurrent requests (default 3), stash/unstash on slot freed

## 4. MatchMagic Actor

- [x] 4.1 Implement MatchMagicManager singleton actor — holds Dictionary<string, RuleSet> state, handles LoadRuleSet/UnloadRuleSet
- [x] 4.2 Add ScoreItems handling — map ScoreCandidate[] to MatchMagic MediaItem[], evaluate with loaded RuleSet, respond with ScoreCompleted
- [x] 4.3 Add MatchMagicManager tests — scoring with loaded ruleset, scoring with no ruleset (default scores), load/unload lifecycle

## 5. Search Workers

- [x] 5.1 Implement TvSearchWorker sharded entity — build TV-specific MediathekQuery (topic field, duration_min=300), Ask MediathekViewWebManager, PipeTo self
- [x] 5.2 Add MatchMagic scoring step to TvSearchWorker — receive MediathekQueryCompleted, Ask MatchMagicManager, PipeTo self, build SearchCompleted on ScoreCompleted
- [x] 5.3 Add error handling to TvSearchWorker — handle MediathekQueryFailed and Ask timeouts, respond with SearchFailed, passivate
- [x] 5.4 Implement MovieSearchWorker sharded entity — same pattern as TvSearchWorker with movie-specific query (title+topic fields, duration_min=3600)
- [x] 5.5 Add Search worker tests — successful pipeline, mediathek failure, scoring failure, passivation

## 6. Search Gateway

- [x] 6.1 Implement SearchGatewayManager singleton actor — PendingSearch state (Dictionary<SearchId, PendingSearch>), route TvSearchCommand/MovieSearchCommand to correct shard region
- [x] 6.2 Add general search routing — category-based dispatch (5xxx→TV, 2xxx→Movie, none→both)
- [x] 6.3 Add fan-out merge logic — track TvResult/MovieResult in PendingSearch, merge and respond when both arrive
- [x] 6.4 Add timeout handling — scheduled timeout message per SearchId, respond with partial results or SearchFailed on timeout
- [x] 6.5 Add SearchGatewayManager tests — single routing, fan-out merge, partial timeout, failure forwarding

## 7. ArrApi Wiring

- [x] 7.1 Add Cat property to IndexerRequest for Newznab category parameter
- [x] 7.2 Wire IndexerApiEndpoints search/tvsearch/movie handlers to Ask SearchGatewayManager and map SearchResult to Newznab RSS XML
- [x] 7.3 Add ArrApi integration tests — tvsearch routing, movie routing, general search with/without cat

## 8. Host Registration

- [x] 8.1 Register SearchGatewayManager as cluster singleton in AkkaSetupContainer
- [x] 8.2 Register TvSearchWorker and MovieSearchWorker shard regions in AkkaSetupContainer
- [x] 8.3 Register MediathekViewWebManager as cluster singleton in AkkaSetupContainer
- [x] 8.4 Register MatchMagicManager as cluster singleton in AkkaSetupContainer
- [x] 8.5 Register HttpClient for MediathekViewWeb in ServiceSetupContainer
