## Context

The API endpoints across `FunkArr.Api` and `FunkArr.ArrApi` have grown organically. RuleSet endpoints are split across three files that each create their own route group for the same prefix. File and method names don't align with route prefixes or OpenAPI tags. The ArrApi adapter files use role-based names that collide conceptually with internal API file names.

Current state:

| File | Route | Tag |
|------|-------|-----|
| RuleSetApiEndpoints | /api/rulesets | Rulesets |
| RuleSetWriteApiEndpoints | /api/rulesets | Rulesets |
| RuleSetTestApiEndpoints | /api/rulesets | Rulesets |
| QueueApiEndpoints | /api/downloads | Downloads |
| SetupApiEndpoints | /api/health | Health |
| MediathekApiEndpoints | /api/mediathek | Mediathek |
| IndexerApiEndpoints | /index/api | Indexer (Newznab) |
| DownloadApiEndpoints | /download/api | Download Client (SABnzbd) |

## Goals / Non-Goals

**Goals:**
- One endpoint file per route group prefix — no split registrations
- File names, class names, method names, and OpenAPI tags all align with each other and the route prefix
- ArrApi adapters named by protocol (Newznab, SABnzbd) not by role
- Test request parsing extracted from endpoint registration code

**Non-Goals:**
- Changing any endpoint behavior, request/response shapes, or business logic
- Restructuring the project layout beyond endpoint files
- Changing route paths for `/api/rulesets`, `/api/downloads`, `/api/mediathek`, `/index/api`, or `/download/api`
- Moving endpoints between projects

## Decisions

### 1. Merge three RuleSet files into one

Combine `RuleSetApiEndpoints`, `RuleSetWriteApiEndpoints`, and `RuleSetTestApiEndpoints` into a single `RuleSetApiEndpoints.cs` with one `MapRuleSetApi()` method and one `MapGroup("/api/rulesets")` call.

**Why**: All three register routes under the same prefix and tag. Splitting by read/write/test is an implementation concern, not a domain boundary. The merged file is ~250 lines of route registrations (after extracting the parser), which is a comfortable size.

**Alternative considered**: Keep separate files with a shared group variable passed between them. Rejected because it adds coupling without benefit — the files are already tightly coupled through the shared route prefix.

### 2. Extract RuleSetTestRequestParser

Move the ~230 lines of JSON parsing helpers (`ParseTestRequest`, `ParseRules`, `ParseIdentification`, `ParseTitleRules`, `ParseFilterSpec`, `ParseFilterNodes`, `ParseFilterCondition`, `ParseFilterField`, `ParseFilterOp`, `ParseCandidates`) into `RuleSetTestRequestParser.cs` as a static class.

**Why**: This is pure deserialization logic with no routing concerns. Keeping it in the endpoint file inflates the file without adding clarity. The parser is independently testable.

### 3. Rename /api/health → /api/system

The `/api/health` prefix currently serves system diagnostics (setup checks, storage info, cache stats), not health probes. Actual health probes (`/healthz`, `/alive`) live in `ApplicationSetupContainer`. Renaming to `/api/system` eliminates the confusion.

**Why `/api/system` over `/api/diagnostics`**: Shorter, broader, and fits the content (setup checks, storage, cache are system-level concerns, not just diagnostics).

### 4. Protocol-named ArrApi files

Rename `IndexerApiEndpoints` → `NewznabApiEndpoints` and `DownloadApiEndpoints` → `SabnzbdApiEndpoints`. Tags become `"Newznab"` and `"SABnzbd"`.

**Why**: The role-based names (`Indexer`, `Download`) collide with internal API concepts. Protocol names are unambiguous and universally understood in the arr ecosystem. Both files already live in protocol-named namespaces (`FunkArr.ArrApi.Newznab`, `FunkArr.ArrApi.Sabnzbd`).

## Risks / Trade-offs

- **[Breaking route change]** `/api/health/*` → `/api/system/*` breaks the Vue.js frontend. → Mitigation: Update all frontend API calls in the same change. The project is pre-1.0 so breaking changes are acceptable.
- **[Larger merged file]** `RuleSetApiEndpoints.cs` grows to ~250 lines. → Acceptable for a single route group. The parser extraction keeps it focused on routing.
- **[Git history]** File renames lose `git log` continuity unless `git log --follow` is used. → Acceptable trade-off for better naming. Content doesn't change meaningfully, so blame stays useful.
