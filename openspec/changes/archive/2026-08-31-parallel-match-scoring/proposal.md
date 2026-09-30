## Why

The MatchMagicManager is a Cluster Singleton that processes all scoring requests sequentially. Concurrent searches block on each other — with 50 items × N rules × regex evaluations per request, this is a real bottleneck. Additionally, RuleSet file handling (loading, merging community+local) is tangled into the scoring actor, and the matching config uses raw JSON strings where type-safe enums should be.

## What Changes

- **Parallel scoring via Actor Router Pool** — MatchMagicManager delegates scoring to a configurable pool of stateless MatchMagicActors instead of processing inline.
- **New RuleSet domain actors** — RuleSetManager (Singleton) scans ruleset files and activates a RuleSetWorker (Sharded) per ruleset. RuleSetWorker owns file I/O, community+local merging, and pushes resolved config to MatchMagic.
- **RuleSetResolver** — Dedicated Singleton for topic/alias → ruleSetId lookup, queried by SearchWorkers before scoring.
- **Enum-based MatchingConfig messages** — New message types in FunkArr.Messages with proper enums (FilterField, FilterOp, IdentificationStrategy, TitleMatchMode, TitlePartType) replacing string-based config.
- **Strategy consolidation** — 5 identification strategies collapsed to 3: RegexCapture, TitleConstruction, AirdateExtraction.
- **Quality variant building removed from matching** — Not a matching concern; moves to SearchWorker or later pipeline stage.
- **BREAKING**: LoadRuleSet/UnloadRuleSet messages replaced by MatchingConfig push from RuleSetWorker.
- **BREAKING**: ScoreItems now requires a ruleSetId (no more null fallback).

## Capabilities

### New Capabilities
- `matching-config`: Enum-based message model for matching configuration (FilterSpec, IdentificationSpec, MatchingRule, MatchingConfig) — the contract between RuleSet and MatchMagic domains.
- `ruleset-management`: RuleSetManager + RuleSetWorker actors for file scanning, loading, merging, and pushing resolved config. RuleSetResolver for topic/alias lookup.
- `parallel-scoring`: MatchMagicManager with Router Pool of stateless MatchMagicActors for concurrent score evaluation.

### Modified Capabilities
<!-- No existing specs to modify -->

## Impact

- **FunkArr.Messages** — New Scoring message types (MatchingConfig, FilterSpec, IdentificationSpec, enums). Old LoadRuleSet/UnloadRuleSet removed.
- **FunkArr.MatchMagic** — MatchMagicManager rewritten as config store + router. New MatchMagicActor with scoring logic. Existing RuleSet/Rule/Filter types become internal or removed (logic moves to actor).
- **FunkArr.RuleSet** — New actors: RuleSetManager, RuleSetWorker, RuleSetResolver. RuleSetResolver merger logic moves here from MatchMagic.
- **FunkArr.Search** — TvSearchWorker/MovieSearchWorker updated to resolve ruleSetId via RuleSetResolver before scoring.
- **FunkArr.Core** — New actor key interfaces (IRuleSetService, IRuleSetResolver). IMatchMagicService retained.
- **FunkArr (Host)** — Akka setup updated for new actor registrations and Router Pool config.
- **Tests** — MatchMagic and Search tests updated for new message types and actor topology.
