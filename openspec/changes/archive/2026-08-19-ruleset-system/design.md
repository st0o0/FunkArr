## Context

FunkArr's current search pipeline applies generic filters (duration, skip-keywords) identically to all Mediathek results. German broadcaster titles are inconsistent across shows — some embed S##/E## tags, others use dates, absolute episode numbers, or plain episode titles. MediathekArr and RundfunkArr (Node.js) address this with 167+ community-curated rulesets that define per-show matching rules. FunkArr needs an equivalent system that loads community rulesets, auto-generates rules for unknown shows, and allows local overrides.

The existing actor hierarchy has SearchActor (stateless, cache + rate-limit), DownloadQueueActor (persistent), DownloadWorkerActor (transient), and MuxingActor (stateless). The RuleSet system introduces two new actors that integrate at the SearchActor level.

## Goals / Non-Goals

**Goals:**
- Per-show matching rules with five strategies covering all observed Mediathek title patterns
- Three-layer resolution (local > generated > community) so users can override, auto-generation fills gaps, and community provides baseline
- Filesystem-based storage as per-show JSON files in `/config/rulesets/` for transparency and editability
- Auto-generation of rulesets by sampling live Mediathek data and detecting title patterns
- Community rulesets loaded from a configurable GitHub raw URL with periodic refresh

**Non-Goals:**
- Movie matching (separate future change)
- Web UI for ruleset management (JSON files are the interface for now)
- Real-time ruleset sync with MediathekArr's central API (we use the static GitHub file)
- TVDB authentication/token management (existing TvdbClient handles this)

## Decisions

### Decision 1: Full actor model for RuleSet ownership

**Choice**: RuleSetRegistryActor as a top-level actor, RuleSetGeneratorActor as its transient child.

**Alternatives considered**:
- DI service with background refresh: simpler, sync lookup, but breaks the actor-based architecture pattern. SearchActor would hold a direct service reference instead of using message passing.
- Registry as child of SearchActor: ties RuleSet lifecycle to search, but RuleSets are a cross-cutting concern that other actors (e.g., future enrichment) might need.

**Rationale**: Consistent with the existing actor hierarchy. SearchActor already uses async Ask patterns (MediathekClient). The registry actor owns its own state lifecycle — startup loading, periodic refresh, and generation coordination. Registered via Akka.Hosting so any actor can resolve it.

### Decision 2: Filesystem-based storage, not SQLite

**Choice**: Per-show JSON files in `/config/rulesets/{community,generated,local}/`.

**Alternatives considered**:
- SQLite table (like Sonarr/Radarr): transactional, queryable, but opaque — users can't inspect or manually edit rulesets. Copy-paste between instances requires tooling.
- Single JSON file (like RundfunkArr's rulesets.json): simpler I/O, but 167+ shows in one file is unwieldy to edit and diff.

**Rationale**: RuleSets are content-files, not config entities. Users should be able to inspect, copy, and hand-edit them. Per-show files make diffs clean and enable selective overrides. The in-memory index (Dictionary) built at startup provides fast lookup without needing a database. The trade-off is no transactional guarantees for concurrent writes, but only the generator writes to `generated/` and only the refresh writes to `community/` — no contention.

### Decision 3: Community source via GitHub raw content

**Choice**: HTTP GET to a configurable raw GitHub URL (default: RundfunkArr's `data/rulesets.json`), parsed and transformed into per-show JSON files in `community/`.

**Alternatives considered**:
- MediathekArr's paginated API (`mediathekarr.pcjones.de/metadata/api/rulesets.php`): more dynamic, but adds dependency on a third-party API that could go down or change.
- Bundled rulesets in the container image: no network dependency, but stale at build time.

**Rationale**: GitHub raw content is highly available, cacheable, and the URL is configurable. The community directory is a transformed cache — the original flat-array format with JSON-in-JSON strings is parsed once and written as clean per-show files. If GitHub is unreachable, the last written community files serve as fallback.

### Decision 4: Lazy auto-generation trigger

**Choice**: Generate rulesets on-demand when SearchActor requests rules for an unknown TVDB-ID.

**Alternatives considered**:
- Eager generation at startup for all Sonarr-tracked shows: better coverage, but requires Sonarr API integration we don't have.
- Scheduled background scan: wastes resources generating rules for shows nobody searches for.

**Rationale**: Lazy generation ensures we only invest effort for shows the user actually searches for. The first search for an unknown show pays a one-time cost (Mediathek sample query + pattern analysis). Subsequent searches use the generated file.

### Decision 5: Slug-based filenames from topic

**Choice**: Filename is a slugified version of the topic (lowercase, special chars to hyphens, e.g., "Feuer & Flamme" → `feuer-und-flamme.json`). TVDB-ID inside the file is the machine identifier.

**Alternatives considered**:
- TVDB-ID as filename (`329324.json`): unique, but not human-readable.
- Combined (`329324-feuer-und-flamme.json`): verbose.

**Rationale**: The primary use case for the filesystem is human inspection and editing. Slugified topic names are immediately recognizable. The TVDB-ID inside the JSON handles machine lookup. Slug collisions across sources are resolved by the layer priority (local > generated > community).

### Decision 6: RuleSet JSON format — clean, not compatible

**Choice**: Our own clean JSON format with typed fields (not JSON-in-JSON strings).

**Alternatives considered**:
- 1:1 compatible with MediathekArr/RundfunkArr format: zero transformation cost, but the format has `"filters": "[{\"attribute\":...}]"` (JSON strings inside JSON), which is error-prone for hand-editing.

**Rationale**: We parse the community source once during refresh. The transformation cost is trivial. The clean format with proper nested objects is safer for hand-editing and cleaner for deserialization. The `source` and `confidence` fields are our additions that don't exist in the upstream format.

## Data Model

```
RuleSetFile (one JSON file per show)
├── topic: string
├── media: MediaReference
│   ├── tvdbId: int?
│   ├── imdbId: string?
│   ├── name: string
│   └── type: "show"
├── source: "community" | "generated" | "local"
├── confidence: float (0.0 - 1.0)
└── rules: Rule[]
    ├── priority: int (0 = highest)
    ├── filters: Filter[]
    │   ├── field: string ("duration", "title", "description", "topic")
    │   ├── op: "greaterThan" | "lessThan" | "exactMatch" | "contains" | "regex"
    │   └── value: string
    ├── strategy: "seasonAndEpisodeNumber" | "itemTitleExact" | "itemTitleIncludes"
    │             | "itemTitleEqualsAirdate" | "byAbsoluteEpisodeNumber"
    ├── seasonRegex: string? (capture group for season number)
    ├── episodeRegex: string? (capture group for episode number)
    └── titleRules: TitleRule[]
        ├── type: "regex" | "static"
        ├── field: string? ("title", "topic", "description")
        ├── pattern: string? (regex with capture group)
        └── value: string? (literal text for static type)
```

## Actor Architecture

```
ActorSystem
├── SearchActor
│   └── Asks RuleSetRegistryActor for rules before matching
├── RuleSetRegistryActor (registered via Akka.Hosting)
│   ├── State: in-memory index (byTopic + byTvdbId dictionaries)
│   ├── On PreStart: load all JSON files from community/, generated/, local/
│   ├── Scheduled: RefreshCommunity every 60 minutes
│   ├── Messages:
│   │   ├── GetRulesForTopic(topic, tvdbId?) → RulesResponse(rules[])
│   │   ├── RefreshCommunity → fetches GitHub, transforms, writes community/
│   │   ├── GenerationComplete(ruleSetFile) → adds to index
│   │   └── ReloadLocal → re-reads local/ directory
│   └── Child: RuleSetGeneratorActor (created per generation request)
│       ├── Transient, stops after completion
│       ├── Messages:
│       │   └── GenerateRuleSet(tvdbId, showName) → generates + writes file
│       └── Uses: MediathekClient, TvdbClient
├── DownloadQueueActor (existing)
├── DownloadWorkerActor (existing)
└── MuxingActor (existing)
```

## Search Flow with RuleSets

```
Sonarr/Radarr → Newznab tvsearch?tvdbid=329324&season=11&ep=8
    │
    ▼
SearchActor receives TvSearchRequest(tvdbId=329324, season=11, episode=8)
    │
    ├─ Ask RuleSetRegistryActor: GetRulesForTopic(topic=null, tvdbId=329324)
    │   │
    │   ├─ Found in index → return rules
    │   └─ Not found → trigger RuleSetGeneratorActor, return empty for now
    │
    ├─ Query Mediathek with show name (from TVDB or rules topic)
    │
    ├─ If rules exist:
    │   │
    │   │  For each result item:
    │   │   1. Apply skip-keyword filter (accessibility variants)
    │   │   2. Find matching ruleset by topic
    │   │   3. For each rule (sorted by priority):
    │   │      a. Check all filters pass
    │   │      b. Apply matching strategy
    │   │      c. First match → include result with matched episode info
    │   │
    │   └─ Return matched results as SearchResponse
    │
    └─ If no rules: fall back to existing MatchingPipeline
```

## Community Refresh Flow

```
RuleSetRegistryActor receives RefreshCommunity
    │
    ├─ HTTP GET configured RulesetSourceUrl
    │   └─ On failure: log warning, keep existing community/ files
    │
    ├─ Parse flat JSON array (MediathekArr/RundfunkArr format)
    │   ├─ Group by topic
    │   ├─ For each topic group:
    │   │   ├─ Transform filters from JSON-string to typed objects
    │   │   ├─ Transform titleRegexRules from JSON-string to typed objects
    │   │   ├─ Map matchingStrategy string to enum
    │   │   ├─ Collect all rules, sort by priority
    │   │   └─ Build RuleSetFile with source="community", confidence=1.0
    │   └─ Slugify topic → filename
    │
    ├─ Clear community/ directory
    ├─ Write per-show JSON files
    └─ Rebuild in-memory index
```

## Auto-Generation Algorithm

```
RuleSetGeneratorActor receives GenerateRuleSet(tvdbId, showName)
    │
    ├─ 1. Query Mediathek for showName (size=50)
    │
    ├─ 2. Find best matching topic
    │      Exact match → Contains match → Single-topic fallback
    │
    ├─ 3. Filter to topic, remove accessibility variants
    │      Strip: (Audiodeskription), (Gebärdensprache), (klare Sprache)
    │
    ├─ 4. Take first 15 unique results as samples
    │
    ├─ 5. Pattern analysis — count occurrences:
    │      S##/E## patterns (parenthesized, bare, Staffel/Folge)
    │      Date patterns (vom dd. MMMM yyyy, dd.MM.yyyy)
    │      Absolute episode numbers (Episode/Folge/Teil ###)
    │      Topic prefix + separator (: or -)
    │
    ├─ 6. Strategy selection (3+ threshold, priority order):
    │      S/E count > date count & ≥3 → seasonAndEpisodeNumber
    │      Date count > S/E count & ≥3 → itemTitleEqualsAirdate
    │      Absolute ≥3             → byAbsoluteEpisodeNumber
    │      Prefix ≥3 & sep ≥30%   → itemTitleExact
    │      else                    → itemTitleIncludes (fallback)
    │
    ├─ 7. Generate regex patterns from actual sample titles
    │
    ├─ 8. Generate duration filter: greaterThan(median * 0.5)
    │
    ├─ 9. Validate: run generated rules against samples
    │      Match rate >60% → confidence 0.8
    │      Match rate 30-60% → confidence 0.5
    │      Match rate <30% → confidence 0.3, use itemTitleIncludes fallback
    │
    ├─ 10. Write to generated/{slug}.json
    └─ 11. Tell parent: GenerationComplete(ruleSetFile)
```

## Risks / Trade-offs

- **[GitHub unavailability]** → Mitigation: community/ files persist on disk as cache. Only a fresh container with no volume has zero community rules. Log a warning on fetch failure, retry on next refresh cycle.
- **[Auto-generation produces bad rules]** → Mitigation: confidence score flags uncertain rulesets. Low-confidence rules still produce results (itemTitleIncludes is broad), just less precise. Users can override with local/ files.
- **[Topic fragmentation]** (e.g., "Terra X" vs "Terra X History") → Mitigation: topic matching in the registry does exact match first. Sub-brands that don't match any ruleset fall through to auto-generation or generic pipeline. Community rulesets already handle the known cases.
- **[Concurrent filesystem writes]** → Mitigation: only RefreshCommunity writes to community/ (sequential, single actor). Only GeneratorActor writes to generated/ (one child at a time per show). Local/ is user-managed. No contention scenarios.
- **[Large community directory]** (167+ files) → Mitigation: files are small (< 2KB each). Directory listing at startup is fast. In-memory index eliminates per-request I/O.
- **[Breaking upstream format changes]** → Mitigation: we transform on ingest. If the upstream JSON schema changes, only the community parser needs updating. Generated and local files use our own stable format.

## Open Questions

- Should the auto-generator retry after initial failure (e.g., Mediathek API was temporarily down), or wait for the next search request to trigger again?
- Should we expose a health check or status endpoint showing how many rulesets are loaded per source?
