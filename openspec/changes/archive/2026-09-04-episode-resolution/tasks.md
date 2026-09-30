## 1. Project & Infrastructure Setup

- [x] 1.1 Create FunkArr.EpisodeGuide project (class library, net10.0, reference FunkArr.Core only)
- [x] 1.2 Create FunkArr.EpisodeGuide.Tests project (xUnit v3, Microsoft.Testing.Platform)
- [x] 1.3 Add both projects to FunkArr.slnx
- [x] 1.4 Add TvdbOptions (ApiKey string) and EpisodeGuideOptions (DefaultStrategy, DefaultThreshold, DefaultAirdateTolerance) to FunkArr.Core
- [x] 1.5 Add IEpisodeGuideManager marker interface to FunkArr.Core.ActorKeys

## 2. Messages

- [x] 2.1 Create FunkArr.Messages/EpisodeGuide/ namespace with ResolutionConfig record (Strategy string, Threshold float, AirdateTolerance int)
- [x] 2.2 Create EpisodeCandidate record (Index, Title, ConstructedTitle?, AiredAt?, Duration, ExistingSeason?, ExistingEpisode?)
- [x] 2.3 Create ResolvedEpisode record (Index, Season, Episode, EpisodeName, Confidence, Strategy)
- [x] 2.4 Create ResolveEpisodes request (TvdbId int, Season int?, Config ResolutionConfig, Candidates EpisodeCandidate[])
- [x] 2.5 Create EpisodesResolved response (ResolvedEpisode[]) and EpisodeResolutionFailed response (Reason string), both implementing IEpisodeResolutionResponse
- [x] 2.6 Extend MatchingConfig with optional ResolutionConfig? field

## 3. TVDB v4 API Client

- [x] 3.1 Create TvdbClient class with HttpClient injection and TvdbOptions
- [x] 3.2 Implement TVDB v4 authentication — POST /login with API key, store Bearer token, handle 401 with re-auth
- [x] 3.3 Implement GetEpisodesAsync(int seriesId, int? season) — GET /series/{id}/episodes/default, parse response to TvdbEpisode[] (seasonNumber, episodeNumber, name, aired, runtime)
- [x] 3.4 Add response deserialization models (TvdbLoginResponse, TvdbEpisodesResponse, TvdbEpisode)
- [x] 3.5 Write tests for TVDB client (auth flow, episode parsing, error handling)

## 4. Episode Resolution Logic

- [x] 4.1 Create LevenshteinDistance utility in FunkArr.EpisodeGuide — normalized similarity 0-1 with Umlaut normalization
- [x] 4.2 Create EpisodeResolver static class with Resolve(TvdbEpisode[] tvdbEpisodes, EpisodeCandidate[] candidates, ResolutionConfig config) → ResolvedEpisode[]
- [x] 4.3 Implement RegexExtracted strategy — pass-through items with ExistingSeason + ExistingEpisode, confidence 1.0
- [x] 4.4 Implement FuzzyTitleMatch strategy — Levenshtein similarity between candidate Title/ConstructedTitle and TvdbEpisode.Name, accept if above threshold
- [x] 4.5 Implement AirdateMatch strategy — compare candidate AiredAt against TvdbEpisode.Aired with tolerance window
- [x] 4.6 Implement RuntimeWindow tiebreaker — when multiple candidates match, prefer ±35% runtime match
- [x] 4.7 Implement strategy priority chain — try strategies in order, first confident match wins
- [x] 4.8 Write tests for resolution logic (all strategies, edge cases, confidence values, unresolved items)

## 5. EpisodeGuideManager Actor

- [x] 5.1 Create EpisodeGuideManager (ReceiveActor, Cluster Singleton) with TvdbClient and in-memory episode cache
- [x] 5.2 Handle ResolveEpisodes message — check cache for TVDB episodes, fetch if missing, run EpisodeResolver, respond with EpisodesResolved
- [x] 5.3 Implement 12h cache TTL with lazy refresh (re-fetch on cache miss or expiry)
- [x] 5.4 Handle TVDB unavailable — respond with EpisodeResolutionFailed, caller falls back to current behavior
- [x] 5.5 Register EpisodeGuideManager as Cluster Singleton in AkkaSetupContainer
- [x] 5.6 Register TvdbClient and options in ServiceSetupContainer
- [x] 5.7 Write actor tests (cache hit, cache miss, TVDB failure, timeout)

## 6. RuleSet Resolution Config

- [x] 6.1 Extend RuleSetMerger.RawRuleSet with optional Resolution property (RawResolutionConfig: Strategy, Threshold, AirdateTolerance)
- [x] 6.2 Parse resolution config in RuleSetMerger.Build — map to ResolutionConfig record, include in MatchingConfig
- [x] 6.3 Merge resolution config: local overrides community (same as Confidence and other fields)
- [x] 6.4 Update RuleSetWorker to pass ResolutionConfig through MatchingConfig to MatchMagicManager
- [x] 6.5 Write tests for resolution config parsing and merging

## 7. TvSearchWorker Pipeline Extension

- [x] 7.1 Add IEpisodeGuideManager actor ref to TvSearchWorker
- [x] 7.2 After ScoreCompleted: check if any matched items lack Season/Episode in MetadataSpec
- [x] 7.3 If unresolved items exist: construct EpisodeCandidate[] from scored items, get ResolutionConfig from MatchingConfig (via ScoreCompleted), Ask EpisodeGuideManager with ResolveEpisodes
- [x] 7.4 Handle EpisodesResolved — merge resolved Season/Episode into MetadataSpec for each item, then call ToScoredResult
- [x] 7.5 Handle EpisodeResolutionFailed / timeout — proceed with ToScoredResult using existing MetadataSpec (graceful fallback)
- [x] 7.6 If all items already have Season/Episode (regex-matched shows): skip resolution stage, proceed directly to ToScoredResult
- [x] 7.7 Write tests for the extended pipeline (resolution success, failure, skip, partial resolution)

## 8. SearchResultItem Extension

- [x] 8.1 Add ResolutionConfidence (float?) and ResolutionStrategy (string?) to SearchResultItem
- [x] 8.2 Pass resolution metadata through TvSearchWorkerState.ToScoredResult
- [x] 8.3 Update existing search tests if needed

## 9. Configuration & Docker

- [x] 9.1 Add TVDB configuration section to appsettings.json (Tvdb:ApiKey placeholder)
- [x] 9.2 Add FunkArr__Tvdb__ApiKey environment variable to docker-compose.dev.yml
- [x] 9.3 Add EpisodeGuide configuration defaults to appsettings.json
- [x] 9.4 Update community Tatort ruleset with resolution config: strategy "fuzzy", threshold 0.7
