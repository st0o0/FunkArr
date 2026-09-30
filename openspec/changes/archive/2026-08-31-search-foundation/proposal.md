## Why

FunkArr's ArrApi layer (Newznab + SABnzbd) is fully wired but returns stub responses. The core search pipeline — from Prowlarr request to MediathekViewWeb query to scored results — doesn't exist yet. This is the critical path to making FunkArr functional as an indexer.

## What Changes

- Add search actor infrastructure: SearchGatewayManager (singleton) routes `t=tvsearch`, `t=movie`, and `t=search` requests to the appropriate sharded search workers
- Add TvSearchWorker and MovieSearchWorker as sharded entities (SearchId as shard key) for parallel search execution
- Add MediathekViewWebManager (singleton) as the single HTTP gateway to the external API with stashing-based backpressure and rate limiting
- Add MediathekQueryBuilder as an internal utility for 1:1 mapping of query messages to the MediathekViewWeb API format
- Promote MatchMagicManager to a singleton actor that holds loaded RuleSets and accepts scoring requests via Ask
- Add `cat` parameter to IndexerRequest for category-based routing in general search
- Define all search/mediathek/scoring messages as primitive records in FunkArr.Messages

## Capabilities

### New Capabilities

- `search-gateway`: SearchGatewayManager singleton — routes Newznab search types (tvsearch/movie/search) to the correct shard region, manages pending search correlation (SearchId → original sender), handles fan-out merge for category-less general search with timeout
- `tv-search`: TvSearchWorker sharded entity — builds TV-specific MediathekQuery (topic-based, duration >5min), orchestrates MediathekViewWeb query → MatchMagic scoring → SearchCompleted response via PipeTo
- `movie-search`: MovieSearchWorker sharded entity — builds movie-specific MediathekQuery (title+topic, duration >60min), same orchestration pattern as TV search
- `mediathek-gateway`: MediathekViewWebManager singleton — single HTTP client to mediathekviewweb.de/api/query, stashing beyond N concurrent requests, MediathekQueryBuilder for JSON serialization (Content-Type: text/plain quirk)
- `match-scoring`: MatchMagicManager singleton actor — wraps existing pure MatchMagic logic, holds loaded RuleSets in state, accepts ScoreItems requests and responds with ScoreCompleted
- `search-messages`: All message records for the search pipeline — TvSearchCommand, MovieSearchCommand, SearchCompleted, SearchFailed, SearchResultItem, MediathekQuery, MediathekQueryField, MediathekQueryCompleted, MediathekItem, ScoreItems, ScoreCandidate, ScoreCompleted, ScoredItem

### Modified Capabilities

- None

## Impact

- **FunkArr.Messages**: New search, mediathek, and scoring message records
- **FunkArr.Search**: New project content — gateway, workers, mediathek manager, query builder
- **FunkArr.MatchMagic**: New MatchMagicManager actor wrapping existing pure logic
- **FunkArr.ArrApi**: Add `cat` to IndexerRequest, wire IndexerApiEndpoints to SearchGatewayManager
- **FunkArr (host)**: Register new actors in AkkaSetupContainer
- **FunkArr.Core**: ShardMessageExtractor may need IWithSearchId interface
- **External dependency**: HTTP calls to mediathekviewweb.de/api/query (no auth, no documented rate limits)
