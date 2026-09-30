## Context

The Vue 3 frontend (TypeScript + Tailwind CSS 4 + Vite) was built alongside the initial backend implementation but fell out of sync during the mediathek-query-full-integration and matching-engine-rebuild changes. TypeScript interfaces are duplicated inline in every `.vue` file. The frontend routes use `:topic` (string) to identify rulesets, but the API controllers route on `{tvdbId:int}`. The MatchesView has three sub-views (recent, topics, unmatched) but only topics works — recent has no backend endpoint and unmatched is a stub.

## Goals / Non-Goals

**Goals:**
- All frontend types in sync with C# models (channels, no topicTitle)
- Shared TypeScript types file — no more inline duplication
- Routing matches API contracts (tvdbId for shows, imdbId for movies)
- All three MatchesView sub-views functional
- Auto-generation accessible from the UI

**Non-Goals:**
- Redesigning the UI layout or adding new pages
- Changing the API contract (controllers stay as-is)
- Building/deploying wwwroot assets (user's build step)

## Decisions

### Decision 1: Shared types in `types.ts`

**Choice:** Create `src/FunkArr.UI/src/types.ts` with all shared interfaces. Each Vue file imports what it needs. Inline interface definitions are removed.

**Why:** Every Vue file duplicates the same Rule, Filter, FilterGroup, TitleRule, RuleSetFile interfaces. A single source of truth prevents drift and reduces maintenance.

### Decision 2: Route by tvdbId, display topic

**Choice:** Change frontend routes from `/rulesets/:topic` to `/rulesets/:tvdbId`. The RulesetsView navigates using `tvdbId` from the summary data. Detail/Editor views use `tvdbId` in API calls. The topic name is displayed in the UI but not used for routing.

**Why:** The backend controller expects `{tvdbId:int}`. Using topic strings caused routing mismatches that broke CRUD operations.

**Note:** RulesetEditor for "new" rulesets (`/rulesets/new`) stays unchanged — it creates via PUT with the tvdbId from the form.

### Decision 3: Channels display and editing

**Choice:** RulesetDetail shows channels as badges below the topic. RulesetEditor has a comma-separated input field for channels. The TS `RuleSetFile` interface gains `channels?: string[]`.

**Why:** Channels were added to the C# model in the matching-engine-rebuild change. The frontend needs to display and edit them.

### Decision 4: Implement missing Match Intelligence endpoints

**Choice:** Implement `GET /matches/recent` by querying `MatchQualityActor.GetRecentMatches` and returning the records. Implement `GET /matches/unmatched` by querying `MatchQualityActor.GetUnmatchedItems` and returning grouped results.

**Why:** The spec defines these endpoints, the frontend calls them, but the controller has stubs/missing handlers. The MatchQualityActor already has the message handlers — the controller just needs to forward the Ask.

### Decision 5: Auto-generation in RulesetDetail

**Choice:** Add a "Generate Rules" button to RulesetDetail when the source is "generated" or when no rules exist. It calls `POST /api/v1/generate/preview` to preview, displays the result, and offers "Apply" which calls `POST /api/v1/generate/apply`.

**Why:** The GenerateController endpoints exist but have no UI. Users should be able to trigger re-generation from the ruleset detail page.

## Risks / Trade-offs

**[Risk] tvdbId routing breaks bookmarks** → Old bookmarks using `/rulesets/Tatort` will 404. Mitigation: Version 0.x, no deployed users with bookmarks.

**[Risk] MatchQualityActor may not have data yet** → Recent/unmatched endpoints return empty arrays when no matches have been recorded. Mitigation: This is the correct behavior — the UI handles empty states.
