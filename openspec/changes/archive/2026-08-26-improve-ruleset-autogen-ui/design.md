## Context

The RuleSet system has a mature backend — `RuleSetGenerationService` generates rules, `RuleSetMatchingEngine` evaluates with traces, and the API endpoints exist for preview/apply/test. However, the frontend underutilizes these capabilities: the auto-generate flow skips preview, the "New Ruleset" path is fully manual, and the generate/apply endpoint has model gaps (missing TmdbId, hardcoded type). The setup wizard exists but is hidden behind Settings.

Current flow: `RulesetDetail.vue` → `generateRules()` → POST preview → discard result → POST apply (without ruleset body) → reload. This means the preview response is wasted and the apply fails silently for movies.

## Goals / Non-Goals

**Goals:**
- Every RuleSet detail page offers a Generate button, regardless of source
- Generated rules are previewed with test traces before the user commits
- New RuleSet creation starts with MediathekViewWeb search and auto-generation, with manual editing as a follow-up step
- `/generate/apply` correctly handles shows (TvdbId) and movies (TmdbId/ImdbId) with explicit type
- All `MatchIntelligenceController` endpoints return structured errors instead of empty 500s
- Setup problems are immediately visible without navigating to Settings

**Non-Goals:**
- Redesigning the manual RulesetEditor form (it works, just shouldn't be the only entry point)
- Adding new matching strategies or filter types
- Changing persistence DTOs or event schemas
- Multi-step setup wizard improvements (the wizard itself is fine, just the discoverability)

## Decisions

### 1. Generate Preview as inline panel, not modal

Show the generate preview result directly below the Generate button in a collapsible panel (similar to MatchTestPanel). This keeps the user in context and allows comparing generated rules against existing ones side-by-side.

*Alternative considered:* Full-page preview route (`/rulesets/series/:id/generate-preview`). Rejected because it adds routing complexity and loses the context of the current ruleset. A modal was also considered but the content is too rich (multiple rules + test trace table) for a modal.

### 2. Guided creation as a multi-step view, reusing RulesetEditor

The new `/rulesets/new` route becomes a `RulesetCreationWizard.vue` with three steps:
1. **Search** — text input querying MediathekViewWeb via `POST /generate/preview` with `query` only (no IDs yet)
2. **Connect** — show TVDB/TMDB search candidates from the preview result, let user pick or enter ID manually
3. **Preview & Edit** — show generated rules + test traces, with "Edit in Editor" flowing into the existing `RulesetEditor.vue`

*Alternative considered:* Embedding auto-generate into the existing editor form. Rejected because it conflates two different workflows (guided creation vs. editing existing rules).

### 3. Fix apply endpoint with explicit `type` field

Add `TmdbId` and `Type` fields to `GenerateApplyRequest`. The `Type` field (`"show"` or `"movie"`) determines `MediaType` for `SaveLocal`. Fallback: if `Type` is missing, infer from which ID is present (TvdbId → show, ImdbId/TmdbId → movie) for backward compatibility.

The entity key for movies becomes `TmdbId.ToString()` (not ImdbId), matching how `MovieActor` is sharded.

### 4. Error handling via try-catch per endpoint, not global middleware

Add try-catch to each `MatchIntelligenceController` action method, returning `Problem()` (RFC 7807) on failure. This is consistent with how other controllers handle errors (e.g., `GenerateController` catches `AskTimeoutException`).

*Alternative considered:* Global `ProblemDetailsMiddleware`. Would help all controllers, but is a broader change outside this scope. Can be added later.

### 5. Setup banner in App.vue using existing polling data

`App.vue` already polls `/api/v1/setup/status` every 30 seconds and computes a dot color. Extend this to render a dismissible warning banner when `statusDotColor` is red or amber. The banner text varies by problem type (FFmpeg missing, paths not writable, Mediathek unreachable). Clicking the banner navigates to `/setup`.

The banner is dismissible per-session (stored in a `ref`, not persisted). It reappears on page reload if the problem persists.

### 6. Frontend calls preview then shows result before apply

The `generateRules()` function in `RulesetDetail.vue` changes from:
```
preview → apply (discard preview) → reload
```
to:
```
preview → show GeneratePreviewPanel → user clicks Accept → apply (with ruleset) → reload
```

The preview result (`GeneratePreviewResult`) contains the generated `RuleSetFile` and `ItemEvaluation[]` traces. The panel renders these using existing `RuleCard` and `MatchTestPanel` components.

## Risks / Trade-offs

- **MediathekViewWeb rate limiting** → The guided creation flow makes an extra query. Mitigated by reusing the preview endpoint which already queries Mediathek, so it's one request per generation attempt, same as before.
- **Preview data staleness** → Between preview and apply, MediathekViewWeb results may change. Acceptable because the generated rules are pattern-based, not tied to specific items.
- **Dismissible banner reappearing** → Users might find it annoying if setup problems are persistent but intentional (e.g., no FFmpeg in dev). Mitigated by per-session dismiss. Future: add a "don't show again" option that persists to localStorage.
- **Apply backward compatibility** → Adding `Type` field is additive. Existing callers that don't send it get the old inference behavior. No breaking change.
