## Context

The `MediathekSearchQuery` builder was introduced to replace the raw `FetchItems(string, SearchMode)` message with a typed, validated query abstraction. However, it was built against an incomplete understanding of the MediathekViewWeb API. Investigation of the API source code (github.com/mediathekview/mediathekviewweb, `SearchEngine.ts` and `OpenSearchDefinitions.ts`) revealed several unused capabilities: `description` as a searchable field, `duration_min`/`duration_max` range filters, `future` flag for excluding not-yet-aired content, per-query `operator` (`and`/`or`), and a hard size cap of 1000 (our default of 5000 silently truncates).

The search actors (Text, Tv, Movie, Browse) use the builder in a very static way — each calls exactly one entry point with no additional constraints. This change makes the builder API-complete and updates the actors to build richer, more precise queries.

## Goals / Non-Goals

**Goals:**
- Expose all MediathekViewWeb query capabilities through `MediathekSearchQuery`
- Replace `ByFullText` with `Search` that includes `description` in the searched fields
- Add duration filtering, future-exclusion, and operator control
- Fix the size cap mismatch (domain default 200, hard clamp at 1000)
- Update all search actors to use the new capabilities for better result precision

**Non-Goals:**
- Pagination/offset support (API supports it, but no use case yet)
- Migrating RuleSetGeneratorActor/RuleSetActor off direct wire queries (separate change)
- Sort by fields other than timestamp (no use case)
- Breaking the `MediathekGatewayActor` message contract (`QueryItems`/`ItemsQueried` stay as-is)

## Decisions

### Decision 1: `ByFullText` → `Search` with `description` field

**Choice:** Rename `ByFullText` to `Search`, expand searched fields from `["topic", "title"]` to `["topic", "title", "description"]`.

**Why:** "FullText" implies a special search mode, but it's just a multi-field `cross_fields` query. `Search` is honest about what it does. Adding `description` improves recall — movie descriptions often contain the original title, director, and cast names which are valuable for MovieSearchActor.

**Alternative considered:** Keep `ByFullText` and add `description` silently. Rejected because the name is misleading and this is a clean-break opportunity (version 0.x).

### Decision 2: `QueryOperator` enum on entry points, not global

**Choice:** `ByTopic(string, QueryOperator)` and `Search(string, QueryOperator)` accept the operator as a parameter (default `And`). `WithTitle` and `FromChannel` always use `And`.

**Why:** The API sets `operator` per query item, and the primary search term is the only one where `Or` makes sense (e.g., `Search("Tatort Münster", Or)` to match either word). Title and channel filters are always conjunctive — you never want "title matches X OR channel matches Y". Keeping the operator on the entry point matches the API's per-item semantics while preventing nonsensical combinations.

**Alternative considered:** Global `.WithOperator()` on the builder. Rejected because it would apply to all items, making channel/title filters OR-based which is never correct.

### Decision 3: Duration as a filter, not a query field

**Choice:** `WithDuration(int? min, int? max)` maps to top-level `duration_min`/`duration_max` on the wire format, not to a query item with `fields: ["duration"]`.

**Why:** The API treats duration as a range filter (OpenSearch `range` query in the `filter` clause), not as a text search field. The builder mirrors this distinction.

### Decision 4: `ExcludeFuture()` opt-in, not default

**Choice:** Future-exclusion is opt-in via `ExcludeFuture()` rather than being the default with an `IncludeFuture()` opt-out.

**Why:** The `Latest()` entry point is used for browse/RSS feeds where future items might be intentionally included (upcoming shows). Making it opt-in lets each actor decide based on its context. All current search actors will call it, but the builder doesn't force it.

### Decision 5: Size default 200, hard clamp at 1000

**Choice:** Default `MaxResults` drops from 5000 to 200. `Build()` clamps the value to `1..1000` and throws `ArgumentOutOfRangeException` if the input exceeds 1000.

**Why:** The API hard-caps at 1000 — requesting more silently returns only 1000. The old default of 5000 was a silent bug. 200 is a practical default: search results are post-filtered by RuleSet matching and quality scoring, so most items are discarded anyway. Actors that need more can call `.Limit(n)` explicitly. Throwing on >1000 makes the constraint visible at build time rather than silently truncating.

**Exception for `Latest()`:** The `Latest()` entry point keeps its default of 100 (unchanged).

### Decision 6: Wire format DTO extension

**Choice:** Extend `MediathekQueryItem` with `string? Operator` and extend `MediathekQuery` with `int? DurationMin`, `int? DurationMax`, `bool? Future`. All new fields are nullable and serialized with `JsonIgnoreCondition.WhenWritingNull` to maintain backward compatibility.

**Why:** Nullable fields with null-suppression means existing queries produce the exact same JSON as before — the API receives no new fields unless explicitly set. This is a non-breaking wire format change.

## Risks / Trade-offs

**[Risk] `description` field adds noise to search results** → The `description` field may contain text that produces false-positive matches (e.g., "Tatort" mentioned in unrelated show descriptions). Mitigation: RuleSet matching and quality scoring already post-filter results aggressively. If noise becomes a problem, `Search` could be narrowed back to `["topic", "title"]` without API changes.

**[Risk] Duration filter on MovieSearch may be too aggressive** → Setting `min: 2400` (40 minutes) excludes short films and trailers that might be legitimate results. Mitigation: This is a sensible default for feature films from public broadcasters. Can be adjusted per-actor without builder changes.

**[Risk] `ExcludeFuture()` may hide pre-announced content** → Some users may want to see upcoming shows in search results. Mitigation: Opt-in design means this only affects actors that explicitly call it. Browse/RSS could omit it if pre-announced content is desired.

**[Risk] Breaking change: `ByFullText` removal** → Any code calling `ByFullText` will fail to compile. Mitigation: Version is 0.x, clean breaks are acceptable. Only internal callers exist (three actors + tests). The rename is mechanical.
