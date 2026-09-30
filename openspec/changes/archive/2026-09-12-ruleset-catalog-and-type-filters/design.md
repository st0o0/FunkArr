## Context

The community ruleset JSON schema has an optional `media` object with an optional `type` field (`"show"` | `"movie"`, default `"show"`). The API list endpoint projects rulesets into `RuleSetEntry` but omits `media.type`. The UI renders a flat list with search and sort — no type distinction. Two existing community rulesets (`fernsehfilme-und-serien-serien`, `unser-sandmaennchen`) are missing `media` entirely.

The `RegisteredRuleSetEntry` message flows from `RuleSetResolverActor` and already carries `MediaName`. The `RuleSetManager` provides `RuleCount` and `SourceType` via `QueryRuleSetSummaries`. The API endpoint in `RuleSetApiEndpoints.cs` joins these two sources to build the list response.

## Goals / Non-Goals

**Goals:**
- Every community ruleset declares its media type
- The list API exposes `mediaType` so the UI can filter client-side
- The RuleSet list page offers type tabs (All / Shows / Movies) composable with search
- A generated `CATALOG.md` provides a browsable catalog for GitHub visitors

**Non-Goals:**
- Server-side filtering (client-side is sufficient for 200+ items)
- Channel grouping or genre tags
- Virtual scrolling or pagination

## Decisions

### 1. Schema: make `media` and `media.type` required

Make `media` a required property on the root object and add `type` to `mediaReference.required`. This ensures every new community ruleset declares show/movie. The `default: "show"` stays for backward compatibility with local rulesets that might not set it.

**Alternative**: keep optional and infer type from presence of TVDB (show) vs TMDB-only (movie). Rejected — too fragile, many shows have TMDB IDs too.

### 2. MediaType flows through existing enrichment path

`RegisteredRuleSetEntry` gains a `MediaType` field (`string?`). The `RuleSetResolverActor` already reads the parsed config to extract `MediaName` — it reads `MediaType` from the same config. The API projects it as `mediaType` on `RuleSetEntry`.

No new messages or query paths needed — just an additional field on existing ones.

### 3. Client-side type filtering with tab bar

Three tabs above the list: `All (N)` / `Shows (N)` / `Movies (N)`. The active tab filters `filteredRulesets` computed property. Tabs compose with the existing search — searching while on "Movies" tab searches only movies.

Implementation: a `typeFilter` ref (`'all' | 'show' | 'movie'`), applied between `sortedRulesets` and the search filter.

### 4. Type badge on cards

A small pill badge next to the source type badge showing `show` or `movie`. Uses the same styling pattern as the source badge but with distinct colors — `show` in default muted, `movie` in a subtle accent.

### 5. CATALOG.md generation as a PowerShell script

A simple script reads all `data/community/rulesets/*.json` files, groups by `media.type`, and writes a Markdown table to `data/community/CATALOG.md`. Run manually or in CI. No npm/dotnet dependency needed.

**Alternative**: generate in the validate-rulesets CI job. Deferred — can add later if manual generation becomes a pain.

## Risks / Trade-offs

- **Schema tightening breaks local rulesets** → `media` only becomes required in the schema used for CI validation of community rulesets. Local rulesets created via UI already include `media` (the write API sets it). The code handles `null` gracefully.
- **MediaType null for legacy entries** → if a ruleset has no `media.type`, the API returns `null` and the UI treats it as "show" (the schema default). No data loss.
