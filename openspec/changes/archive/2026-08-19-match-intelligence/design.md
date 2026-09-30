## Context

FunkArr's current RuleSet system is functionally equivalent to MediathekArr and RundfunkArr (TypeScript). The rule format supports only AND-combined filters with 4 filterable fields, has no observability into match outcomes, and uses all-or-nothing layer overrides. Accessibility filtering is hardcoded rather than configurable. There is no way to know why a search produced or failed to produce matches.

This change introduces two capabilities: a redesigned rule format with full filter logic, and an in-memory Match Ledger that records and exposes match outcomes via API.

## Goals / Non-Goals

**Goals:**
- Redesign the rule format to support AND/OR/NOT filter composition, per-rule confidence, topic aliasing, channel filtering, and merge-based overrides
- Record every search's match process in an in-memory ledger with per-rule trace information
- Expose match data via REST API for observability (recent matches, topic stats, unmatched items)
- Maintain backward compatibility with the community ruleset format (parse old into new model)

**Non-Goals:**
- Persist match ledger data across restarts (explicitly in-memory only)
- Build a Web UI (API-first, UI can come later or via Grafana)
- Change the Newznab or SABnzbd API surfaces
- Modify the download pipeline or muxing system
- Add new matching strategies (the 5 existing strategies remain)

## Decisions

### 1. Composite filter tree instead of flat filter list

**Decision:** Replace `IReadOnlyList<Filter>` with a `FilterGroup` that supports `all` (AND), `any` (OR), and `not` (negate) composition.

**Rationale:** The current AND-only model forces accessibility filtering to be hardcoded in the engine. With composite filters, accessibility exclusions become configurable rules (`not: [{ field: title, op: contains, value: "Audiodeskription" }]`). This also enables channel-specific rules ("only match ARD content") which is impossible today.

**Alternative considered:** Adding `exclude` as a separate field on Rule alongside filters. Rejected because it's a half-measure — you still can't express OR logic, and two filter lists with different semantics is confusing.

**Structure:**
```
FilterGroup:
  all: FilterNode[]    # AND — all must pass (default, backward compat)
  any: FilterNode[]    # OR — at least one must pass  
  not: FilterNode[]    # NOT — none may pass

FilterNode = Filter | FilterGroup  # recursive composition
```

### 2. Topic aliasing via aliases array

**Decision:** Add an `aliases` string array to `RuleSetFile`. The registry indexes all aliases alongside the primary topic.

**Rationale:** German broadcasters use inconsistent topic names ("Tatort" vs "Tatort - Münster"). Currently this requires duplicate rulesets. Aliases let one ruleset cover all variants.

**Alternative considered:** Regex-based topic matching. Rejected — too error-prone and hard to debug. Explicit aliases are predictable.

### 3. Merge-based layer overrides

**Decision:** Add an optional `overrides` section to local rulesets with `mode: merge | replace`. Merge mode adds/removes specific rules from the base layer instead of replacing the entire ruleset.

**Rationale:** Today, creating a local override means duplicating the entire community ruleset plus your changes. If the community ruleset improves, your local copy is stale. Merge mode lets users add one rule or remove one rule while inheriting the rest.

**Alternative considered:** Rule-level priority across layers (community rule at priority 10 + local rule at priority 5). Rejected because it conflates intra-ruleset priority with inter-layer priority and makes behavior hard to predict.

### 4. MatchLedgerActor for in-memory recording

**Decision:** Implement the Match Ledger as a stateless Akka.NET `ReceiveActor` holding a `CircularQueue<MatchRecord>` (from Servus.Core). The SearchActor tells match events to the ledger after each search.

**Rationale:** An actor fits the existing architecture, provides thread-safe writes from concurrent searches, and Servus's CircularQueue gives bounded memory with automatic eviction. No persistence needed — the data is ephemeral.

**Alternative considered:** Serilog structured logging + Loki queries. Rejected as primary mechanism — logging is good for debugging but doesn't enable aggregated stats or API queries. However, match events SHOULD also be logged at Debug level for Loki correlation.

**Retention:** CircularQueue with configurable capacity (default: 10,000 entries). At ~1KB per entry, this is ~10MB memory. Old entries are evicted automatically.

### 5. Match trace as value object, not event stream

**Decision:** Each match attempt produces a `MatchTrace` record containing the full evaluation path: which rules were tried, which filters passed/failed, which strategy was applied, and the outcome. This is attached to the `MatchRecord` stored in the ledger.

**Rationale:** A trace-per-item is simpler than an event stream and directly answers "why did this item match/not match?" without requiring event correlation.

### 6. Filterable fields expanded to 6

**Decision:** Add `channel` and `timestamp` to the existing 4 filterable fields (duration, title, description, topic).

**Rationale:** Channel filtering enables "only ARD" or "only ZDF" rules. Timestamp filtering enables "only items from the last N days" which helps with freshness-based rules.

### 7. Per-rule confidence instead of per-ruleset only

**Decision:** Move `confidence` from `RuleSetFile` to `Rule`, keep the file-level confidence as a default that individual rules can override.

**Rationale:** A ruleset can have a high-confidence regex rule for S/E extraction and a low-confidence fallback. File-level confidence alone cannot express this.

### 8. Backward-compatible community parsing

**Decision:** The `CommunityRuleSetParser` continues to parse the upstream flat-JSON format but maps it into the new model. Old-format filters become `all: [...]` groups. No `any`/`not`/`aliases`/`overrides` in community format (those are local/generated features).

**Rationale:** Community rulesets are maintained externally. FunkArr must consume them as-is. The new format is for local overrides, generated rulesets, and future FunkArr-native community format.

## Risks / Trade-offs

- **Recursive filter trees add complexity to the matching engine** → Mitigated by keeping the tree shallow in practice (max 2-3 levels). Engine validates depth on load.
- **Match Ledger memory consumption with high search volume** → Mitigated by CircularQueue with fixed capacity. 10K entries × ~1KB = ~10MB ceiling.
- **Breaking change to RuleSetFile record** → Mitigated by backward-compatible parsing. Old JSON files load into new model. On-disk format migration happens lazily (regenerated files use new format, community files are parsed on load).
- **Match API adds unauthenticated surface area** → Match Intelligence API endpoints SHALL require the same ApiKey authentication as existing APIs.

## Open Questions

- Should the match trace include the actual Mediathek result item data (title, URL, duration), or just a reference? Full data is more useful for debugging but increases memory. Leaning toward full data given the 10K cap.
- Should unmatched items in the ledger be grouped by topic for faster aggregation, or is linear scan acceptable at 10K entries? Starting with linear scan, optimize if profiling shows need.
