## Why

Enrichment config (enabled, methods, title/airdate/runtime/year thresholds) exists in ruleset JSON files and flows through the backend pipeline via RuleSetMerger into actors, but is completely invisible in the UI. The API layer doesn't return it, the Builder can't edit it, the test endpoint doesn't run it, and the debugger doesn't show enrichment results. Users must hand-edit JSON files to tune enrichment, and have no way to see how threshold changes affect matching outcomes.

## What Changes

- Expose enrichment config through the ruleset detail API and accept it in create/update requests
- Add an "Enrichment" section to the RuleSet Builder form with controls for enabled toggle, method selection, and per-method threshold/tolerance inputs
- Extend the test scoring endpoint to optionally run enrichment after scoring, using the existing EnrichmentManager actor
- Extend ItemTrace with enrichment trace data (method used, similarity score, resolved episode/movie details)
- Show enrichment results in the LiveMatchPreview debugger's full test mode, so users can tune thresholds and immediately see updated results

## Capabilities

### New Capabilities

- `enrichment-ui-builder`: Builder form section for viewing and editing enrichment config (enabled, methods, thresholds)
- `enrichment-test-endpoint`: Test scoring endpoint extension that runs enrichment after scoring and returns enrichment traces
- `enrichment-debugger-display`: LiveMatchPreview enrichment result display in full test mode

### Modified Capabilities

- `enrichment-config`: Adding API-layer models and serialization for enrichment config (currently only internal to RuleSetMerger)
- `ruleset-builder-ui`: Adding enrichment section to the builder form and wiring enrichment config into save/load
- `ruleset-debugger-ui`: Extending full test mode to show enrichment results alongside scoring traces
- `scoring-trace-model`: Extending ItemTrace with optional enrichment trace fields
- `ruleset-api`: Extending detail/create/update endpoints to include enrichment config

## Impact

- **Backend**: Messages (RuleSetDetailResult, TestScoreItems, ItemTrace), API models and endpoints, mapping extensions
- **Frontend**: rulesets.ts API client, RuleSetBuilder.vue, LiveMatchPreview.vue, new EnrichmentTraceView component
- **External APIs**: Test endpoint will make real TVDB/TMDB API calls when enrichment is enabled in test mode
- **No breaking changes**: All enrichment fields are optional additions to existing API contracts
