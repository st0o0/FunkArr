## 1. Backend: System version endpoint

- [x] 1.1 Add `GET /api/system/version` endpoint in `SystemApiEndpoints` that returns `{ appVersion, communityRulesetVersion }` — read version.txt via `IDataFiles`, assembly version via reflection
- [x] 1.2 Add response record `VersionResponse(string AppVersion, string? CommunityRulesetVersion)` to `FunkArr.Api.Models`
- [x] 1.3 Add output caching to the version endpoint

## 2. Backend: Rulesets list response wrapper

- [x] 2.1 Change `GET /api/rulesets` response from array to object wrapper `{ communityVersion, rulesets }` — read version.txt in `RuleSetApiEndpoints`
- [x] 2.2 Add response record `RuleSetListResponse(string? CommunityVersion, IReadOnlyList<RuleSetSummary> Rulesets)`

## 3. Frontend: API types and fetch

- [x] 3.1 Add `getSystemVersion()` fetch function and `VersionResponse` type to a new `api/system.ts` (or extend `api/setup.ts`)
- [x] 3.2 Update `api/rulesets.ts` — adjust the rulesets list fetch to unwrap the new `{ communityVersion, rulesets }` response shape

## 4. Frontend: Sidebar version display

- [x] 4.1 In `AppLayout.vue`, fetch `GET /api/system/version` on mount and store `communityRulesetVersion`
- [x] 4.2 Render version text (`Rulesets v1.2.0`) below the Collapse toggle in `text-xs text-text-muted` — hidden when collapsed or version is null

## 5. Frontend: RuleSets page version badge

- [x] 5.1 In the RuleSets list view, extract `communityVersion` from the rulesets API response
- [x] 5.2 Render a `v1.2.0` badge in `text-xs text-text-secondary` near the page title — hidden when null

## 6. Verify

- [x] 6.1 Build and run dev stack, verify version appears in sidebar and RuleSets page
- [x] 6.2 Verify version is null on fresh install (delete version.txt), confirm no UI artifacts
