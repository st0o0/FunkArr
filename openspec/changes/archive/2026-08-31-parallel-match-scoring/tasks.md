## 1. Message Types (FunkArr.Messages)

- [x] 1.1 Add enums to FunkArr.Messages/Scoring: FilterField, FilterOp, IdentificationStrategy, TitleMatchMode, TitlePartType
- [x] 1.2 Add FilterCondition record
- [x] 1.3 Add FilterNode abstract record with ConditionNode and GroupNode, and FilterSpec record
- [x] 1.4 Add TitlePart record
- [x] 1.5 Add IdentificationSpec record
- [x] 1.6 Add MatchingRule and MatchingConfig records
- [x] 1.7 Add RuleSet domain messages: RegisterRuleSet, ResolveRuleSet, RuleSetResolved, RuleSetNotFound
- [x] 1.8 Remove old LoadRuleSet and UnloadRuleSet messages
- [x] 1.9 Update ScoreItems to require non-null ruleSetId (change from `string?` to `string`)

## 2. RuleSet Domain Actors (FunkArr.RuleSet)

- [x] 2.1 Add actor key interfaces to FunkArr.Core: IRuleSetService, IRuleSetResolver
- [x] 2.2 Implement RuleSetResolver actor (Singleton) — in-memory topic/alias → ruleSetId lookup, handles RegisterRuleSet and ResolveRuleSet messages
- [x] 2.3 Implement RuleSetWorker actor (Sharded by ruleSetId) — loads community + local JSON, merges via resolve logic, transforms string→enum, sends MatchingConfig to MatchMagicManager and RegisterRuleSet to RuleSetResolver
- [x] 2.4 Move RuleSetResolver merge logic (community+local) from FunkArr.MatchMagic to FunkArr.RuleSet
- [x] 2.5 Implement RuleSetManager actor (Singleton) — scans data/community/rulesets/ and data/local/rulesets/, activates RuleSetWorker per discovered ruleSetId

## 3. MatchMagic Redesign (FunkArr.MatchMagic)

- [x] 3.1 Add internal ExecuteScoring record (MatchingConfig, ScoreCandidate[], IActorRef ReplyTo)
- [x] 3.2 Implement MatchMagicActor (stateless) — filter evaluation, identification (RegexCapture, TitleConstruction, AirdateExtraction), priority ordering, replies ScoreCompleted to replyTo
- [x] 3.3 Rewrite MatchMagicManager — config dictionary, SmallestMailboxPool of MatchMagicActors, routes ScoreItems → ExecuteScoring, handles MatchingConfig storage
- [x] 3.4 Remove old types that are now superseded: RuleSet.cs, Rule.cs, RuleSetResolver.cs (merge logic moved), FilterOp.cs (moved to Messages), MatchStrategy.cs (replaced by IdentificationStrategy)
- [x] 3.5 Evaluate which remaining types (Filter.cs, FilterGroup.cs, MediaItem.cs, MediaRef.cs, etc.) are still needed or can be removed

## 4. Search Integration (FunkArr.Search)

- [x] 4.1 Update TvSearchWorker to resolve ruleSetId via RuleSetResolver before sending ScoreItems
- [x] 4.2 Update MovieSearchWorker to resolve ruleSetId via RuleSetResolver before sending ScoreItems
- [x] 4.3 Handle RuleSetNotFound response — return results with all items scored at 0.0

## 5. Host Wiring (FunkArr)

- [x] 5.1 Register RuleSetManager, RuleSetResolver as Singletons and RuleSetWorker ShardRegion in AkkaSetupContainer
- [x] 5.2 Update MatchMagicManager registration to use SmallestMailboxPool with configurable pool size
- [x] 5.3 Add pool size configuration to appsettings.json

## 6. Tests

- [x] 6.1 Add MatchMagicActor unit tests — filter evaluation (flat, nested, not), all three identification strategies, priority ordering, confidence fallback
- [x] 6.2 Add MatchMagicManager unit tests — config storage, routing to pool, unknown ruleSetId handling
- [x] 6.3 Add RuleSetResolver unit tests — registration, topic lookup, alias lookup, unknown topic, overwrite
- [x] 6.4 Add RuleSetWorker unit tests — JSON loading, community+local merge, string→enum transformation, standalone/disable handling
- [x] 6.5 Update TvSearchWorker tests for RuleSetResolver integration
- [x] 6.6 Update MovieSearchWorker tests for RuleSetResolver integration
- [x] 6.7 Verify existing strategy test coverage maps to new 3-strategy model (run existing MatchMagic tests, adapt as needed)

## 7. Cleanup

- [x] 7.1 Remove unused types from FunkArr.MatchMagic after migration
- [x] 7.2 Run dotnet format and fix any violations
- [x] 7.3 Run all tests and verify green
