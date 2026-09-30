## Why

The RuleSet auto-generation workflow is fragmented and partially broken. The Generate button only appears for `source === 'generated'` rulesets, there is no preview step before applying generated rules, the "New Ruleset" flow is fully manual with no auto-generate option, and `/generate/apply` silently fails for movies identified by TmdbId (missing field in request model) and hardcodes the media type instead of deriving it. Additionally, `/matches/recent` returns a 500 with empty body due to missing error handling, and the setup wizard is buried 2 clicks deep behind the settings page.

## What Changes

- **Universal Generate button**: Show "Generate Rules" on every RulesetDetail page regardless of source type, not just `source === 'generated'`
- **Generate preview step**: Show generated rules and test traces in an inline panel before applying, with Accept/Edit/Discard options instead of auto-applying
- **Guided "New Ruleset" flow**: Replace the bare manual editor with a step-based flow: search MediathekViewWeb → pick TVDB/TMDB match → auto-generate → preview + test → edit/save
- **Fix `/generate/apply` type handling**: Add `TmdbId` to `GenerateApplyRequest`, derive `MediaType` from an explicit `type` field instead of hardcoding based on which ID is present, pass the generated RuleSet from preview to apply in the frontend
- **Fix `/matches/recent` 500**: Add try-catch with meaningful error responses to all `MatchIntelligenceController` endpoints
- **Setup status visibility**: Surface setup problems via a persistent banner in the header when status is degraded, linking directly to the setup wizard

## Capabilities

### New Capabilities
- `generate-preview-panel`: Inline panel component showing generated rules with test traces, confidence score, and accept/edit/discard actions — used in both the detail page re-generate flow and the new ruleset creation flow
- `guided-ruleset-creation`: Step-based "New Ruleset" wizard replacing the bare editor for initial creation — MediathekViewWeb search, TVDB/TMDB connection, auto-generation, preview, then manual edit
- `setup-status-banner`: Persistent header banner surfacing setup problems with direct link to the setup wizard, replacing the subtle dot-only indicator

### Modified Capabilities
- `ruleset-generation-api`: Add `TmdbId` to apply request, add explicit `type` field, derive MediaType correctly
- `match-intelligence-api`: Add error handling to all endpoints, return structured error responses instead of empty 500s
- `web-ui-shell`: Add setup status banner rendering in the app shell header
- `ruleset-api`: Support testing rules from the generate preview flow (ad-hoc rules, not yet persisted)

## Impact

- **Backend**: `GenerateController.cs` (apply request model + type derivation), `MatchIntelligenceController.cs` (error handling), `FunkArrApplicationSetup.cs` (optional global error middleware)
- **Frontend**: `RulesetDetail.vue` (universal generate + preview panel), `RulesetEditor.vue` (guided creation flow), `App.vue` (setup banner), new components (`GeneratePreviewPanel.vue`, `RulesetCreationWizard.vue`)
- **API contract**: `GenerateApplyRequest` record gains `TmdbId` and `Type` fields — additive, no breaking change
- **OpenAPI**: Updated request/response schemas for generate endpoints
