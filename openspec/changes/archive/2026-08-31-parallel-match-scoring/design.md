## Context

MatchMagicManager is currently a Cluster Singleton that holds all loaded RuleSets in memory and processes ScoreItems requests sequentially. RuleSet file handling (JSON loading, community+local merging) is external — raw JSON is pushed via LoadRuleSet messages. The matching config uses string-based fields internally, and 5 identification strategies exist where 3 would suffice.

The RuleSet domain project exists but has no actors yet. All ruleset lifecycle is handled outside the domain boundary.

## Goals / Non-Goals

**Goals:**
- Enable parallel scoring via Actor Router Pool (configurable concurrency)
- Move ruleset file ownership into the RuleSet domain (RuleSetManager + RuleSetWorker)
- Provide a dedicated topic/alias → ruleSetId resolver (RuleSetResolver)
- Define a clean, enum-based MatchingConfig message contract between RuleSet and MatchMagic domains
- Consolidate 5 identification strategies to 3 (RegexCapture, TitleConstruction, AirdateExtraction)

**Non-Goals:**
- Persistence for RuleSet actors (file system is the source of truth)
- File watching / hot-reload of rulesets (future enhancement)
- Changing the Newznab/SABnzbd API surface
- Modifying the ruleset JSON schema (existing files remain compatible)
- Quality variant building redesign (removed from matching, final location TBD)

## Decisions

### 1. Router Pool over Cluster Sharding for MatchMagicActors

**Choice:** `SmallestMailboxPool` with configurable pool size.

**Alternatives considered:**
- *Cluster Sharding by ruleSetId* — Each show gets its own actor. Overhead of shard coordination is disproportionate; most scoring requests are short-lived CPU bursts, not long-lived stateful entities.
- *Singleton (status quo)* — Sequential processing. Bottleneck under concurrent searches.

**Rationale:** MatchMagicActors are stateless — they receive everything they need in the message (MatchingConfig + candidates). A pool of identical workers with smallest-mailbox routing distributes load without coordination overhead. Pool size is configurable: 1 behaves like today, N enables parallelism.

### 2. Stateless MatchMagicActor with config-per-message

**Choice:** MatchMagicManager holds the config dictionary and bundles the resolved MatchingConfig into each `ExecuteScoring` message sent to the pool.

**Alternatives considered:**
- *Each pool actor caches config* — Adds state synchronization complexity. Every config update must fan out to all pool members.
- *Pool actors fetch config from Manager on demand* — Adds a round-trip per scoring request.

**Rationale:** Config changes are infrequent (startup + file changes). Scoring requests are frequent. Bundling config into the message keeps pool actors purely stateless with zero coordination.

### 3. RuleSetWorker sharded by ruleSetId

**Choice:** One RuleSetWorker per ruleset file, managed by RuleSetManager Singleton.

**Rationale:** Each ruleset file has independent lifecycle (load, merge community+local, push config). Sharding by ruleSetId gives natural isolation. RuleSetManager scans directories and activates workers.

### 4. Separate RuleSetResolver Singleton

**Choice:** Dedicated actor for topic/alias → ruleSetId resolution, separate from RuleSetManager.

**Alternatives considered:**
- *RuleSetManager handles lookup directly* — Mixes file lifecycle management with query serving.
- *SearchWorker knows ruleSetId upfront* — Requires API layer to resolve before creating search, pushing domain logic into adapters.

**Rationale:** Single responsibility. RuleSetResolver receives registrations from RuleSetWorkers at startup and serves fast in-memory lookups. SearchWorkers ask it before sending ScoreItems.

### 5. Three identification strategies instead of five

**Choice:**
- `RegexCapture` — Merges SeasonAndEpisodeNumber + ByAbsoluteEpisodeNumber. Season pattern is optional; if absent, only episode is captured.
- `TitleConstruction` — Merges ItemTitleExact + ItemTitleIncludes. Uses `TitleMatchMode` enum (Exact | Contains) to distinguish.
- `AirdateExtraction` — Unchanged, hardcoded German date parsing.

**Rationale:** The original split created strategy variants that differed only in optional fields (season regex present or not) or comparison mode (== vs Contains). Enum flags express these orthogonal choices better than strategy multiplication.

### 6. MatchingConfig as the domain boundary contract

**Choice:** New message types in FunkArr.Messages with enums. MatchMagicActor operates directly on these types (Approach C — no separate engine types, no mapping layer).

**Alternatives considered:**
- *Approach A: Map MatchingConfig → internal types* — Mapping layer, two representations of the same data.
- *Approach B: Logic on message types* — Violates "Messages has no dependencies."

**Rationale:** The worker IS the engine. MatchingConfig is pure data; the actor has the algorithms. No duplication, no mapping, clean separation.

### 7. FilterSpec with recursive nesting

**Choice:** `FilterNode` abstract hierarchy — `ConditionNode(FilterCondition)` | `GroupNode(FilterSpec)`. FilterSpec holds `FilterNode[]?` for All/Any/Not.

**Rationale:** Some rulesets need nested filter groups (e.g., "duration > 40 AND (channel == ZDF OR channel == ARD)"). Flat conditions can't express this. The recursive structure matches the existing JSON schema capability.

## Risks / Trade-offs

**[Pool sizing]** → Wrong pool size wastes resources (too large) or still bottlenecks (too small). Mitigation: Default to reasonable value (e.g., 4), make configurable via appsettings. SmallestMailboxPool adapts naturally to load.

**[Config bundling adds message size]** → Each ExecuteScoring carries a full MatchingConfig copy. Mitigation: MatchingConfig is small (a handful of rules with regexes). The overhead is negligible compared to regex evaluation cost.

**[RuleSetResolver stale data]** → If a RuleSetWorker updates topic/aliases after initial registration, the resolver could serve stale mappings. Mitigation: RuleSetWorker re-sends registration on every config change. Resolver overwrites entries idempotently.

**[Breaking message changes]** → LoadRuleSet/UnloadRuleSet removal and ScoreItems requiring ruleSetId breaks existing consumers. Mitigation: Version 0.x — clean breaks are acceptable, no migration needed.

**[Strategy consolidation changes behavior]** → Merging strategies could introduce subtle differences. Mitigation: Existing MatchMagic tests validate all strategy behaviors. Strategy consolidation preserves identical code paths, just reorganized under fewer enum values.
