## Why

The RuleSetRegistryActor keeps local overrides in a volatile in-memory dictionary that is lost on restart. The catalog endpoint (`GET /api/v1/rulesets`) never shows locally saved rulesets because recovery does not restore them. Save operations use fire-and-forget `Tell`, causing race conditions where the API returns before persistence completes (frontend crash on detail page). Additionally, the match-intelligence recent endpoint is missing, API key authentication is not enforced, and there is no way to export a ruleset as JSON for community contribution.

## What Changes

- **RuleSetRegistryActor becomes event-sourced** (`ReceivePersistentActor`) so the catalog (community + local + generated entries) survives restarts. Events: `CommunityBatchLoaded`, `LocalRegistered`, `LocalRemoved`.
- **Save/Delete flows become Ask-based** — the controller awaits registry persistence confirmation before responding, eliminating the fire-and-forget race condition (fixes detail page crash).
- **GenerateController.Apply registers with the registry** — auto-generated rulesets that are applied as local overrides now appear in the catalog.
- **Ruleset export endpoint** — `GET /api/v1/rulesets/{id}/export` returns the effective `RuleSetFile` as a downloadable JSON file. UI gets an export button on the detail page.
- **Match-intelligence recent endpoint** — implement `GET /api/v1/matches/recent` so the Recent tab works instead of hitting the SPA fallback.
- **API key authentication middleware** — enforce `FunkArr:ApiKey` on all API endpoints (Newznab, SABnzbd, REST API). Caps endpoint exempted per Newznab convention.
- **RulesetDetail.vue response unwrapping** — fix the `RuleSetResponse` → `RuleSetFile` destructuring so `ruleset.media.name` resolves correctly.

## Capabilities

### New Capabilities

- `ruleset-export`: Download a ruleset as JSON file from the API and UI for community contribution.
- `api-authentication`: Middleware that enforces API key validation on all protected endpoints.

### Modified Capabilities

- `ruleset-registry`: Registry becomes event-sourced; catalog includes local overrides and survives restart.
- `ruleset-api`: Save/Delete become Ask-based; export endpoint added; GenerateController registers locals.
- `match-intelligence-api`: Add `GET /matches/recent` endpoint returning recent match records.
- `api-contracts`: RulesetDetail.vue fix for RuleSetResponse unwrapping.

## Impact

- **RuleSetRegistryActor** — changes base class to `ReceivePersistentActor`, adds persistence ID, events, recovery logic, snapshot support.
- **RulesetController** — Save/Delete change from Tell to Ask pattern. New export action.
- **GenerateController** — Apply now sends `RegisterLocal` to registry.
- **FunkArrApplicationSetup** — new API key authentication middleware in the pipeline.
- **FunkArrActorSystemSetup** — registry actor persistence ID registration.
- **MatchIntelligenceController** — implement `GetRecent` with actual data.
- **RulesetDetail.vue** — fix response unwrapping, add export button.
- **MatchesView.vue** — works once backend endpoint exists.
- **Persistence journal** — new event types for registry; existing ShowActor/MovieActor events unchanged.
