## Why

FunkArr has no UI for inspecting or understanding rulesets. Tuning matching behavior requires reading JSON files and interpreting log output. A web UI showing registered rulesets, their merged configs, and scoring history gives immediate visibility into what rules exist, how they layer, and how they perform against real searches.

## What Changes

- Add query messages to RuleSetResolver and RuleSetManager so the API can read ruleset state
- Build internal REST API endpoints in FunkArr.Api (`/api/rulesets/*`) that expose ruleset data and scoring history
- Build Vue UI pages: dashboard landing, ruleset list, ruleset detail (identity + source + merged config + rules), scoring history, scoring detail with item traces
- Wire up static file serving and SPA fallback in the host for the Vue frontend

## Capabilities

### New Capabilities

- `ruleset-query-messages`: New query/response messages for listing registered rulesets and retrieving ruleset detail (source info + merged MatchingConfig) from actors
- `ruleset-api`: Internal REST API endpoints for rulesets and scoring history (`GET /api/rulesets`, `GET /api/rulesets/:id`, `GET /api/rulesets/:id/history`, `GET /api/rulesets/:id/history/:rid`)
- `ruleset-ui`: Vue frontend pages for dashboard, ruleset list, ruleset detail, scoring history, and scoring detail
- `static-file-serving`: Host configuration for serving Vue dist/ as static files with SPA fallback

### Modified Capabilities

- `ruleset-management`: RuleSetResolver gains `QueryRegisteredRuleSets` handler to list all registered rulesets
- `ruleset-management`: RuleSetManager gains `QueryRuleSetDetail` handler to return source metadata + merged MatchingConfig for a single ruleset
- `application-bootstrap`: ApplicationSetupContainer maps new API endpoints and static file middleware

## Impact

- **FunkArr.Messages**: New query/response records for ruleset listing and detail
- **FunkArr.RuleSet**: RuleSetResolver and RuleSetManager handle new query messages
- **FunkArr.Api**: New endpoint classes for ruleset and scoring history routes
- **FunkArr (host)**: ApplicationSetupContainer gains static file serving + API mapping
- **FunkArr.UI**: New Vue pages, router config, API client
- **No breaking changes** to existing Newznab/SABnzbd APIs or actor behavior
