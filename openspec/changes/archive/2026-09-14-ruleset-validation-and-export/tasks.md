## 1. Schema Embedding and Validator Service

- [x] 1.1 Add `JsonSchema.Net` package to `Directory.Packages.props` and `FunkArr.RuleSet.csproj`
- [x] 1.2 Embed `data/community/ruleset.schema.json` as linked embedded resource in `FunkArr.RuleSet.csproj`
- [x] 1.3 Create `IRuleSetValidator` interface in `FunkArr.Core` with `Validate(string json)` returning a list of validation errors
- [x] 1.4 Create `RuleSetValidator` in `FunkArr.RuleSet` — load schema from embedded resource, cache parsed schema, implement JSON schema validation
- [x] 1.5 Add business rule validation: confidence range (0–1), strategy enum check, regex pattern syntax check
- [x] 1.6 Implement human-readable error message mapping — extract rule ID/index from JSON path, compose contextual messages with fix instructions
- [x] 1.7 Register `IRuleSetValidator` as singleton in `RuleSetSetupContainer`
- [x] 1.8 Write tests for `RuleSetValidator` — valid ruleset, missing fields, wrong types, confidence range, invalid strategy, invalid regex, multiple errors at once

## 2. API Validation on Save

- [x] 2.1 Add validation call in `HandleCreate` (`POST /api/rulesets`) — validate before writing, return 422 with errors array on failure
- [x] 2.2 Add validation call in `HandleUpdate` (`PUT /api/rulesets/{id}`) — validate before writing, return 422 with errors array on failure
- [x] 2.3 Define validation error response model `{ "errors": [{ "field": "...", "message": "..." }] }`
- [x] 2.4 Write API tests for validation — skipped: no WebApplicationFactory infrastructure, existing API tests only cover endpoint registration

## 3. Community Export

- [x] 3.1 Create `RuleSetExporter` in `FunkArr.RuleSet` — read community + local JSON, call `RuleSetMerger.Resolve()`, serialize to canonical JSON format, strip `standalone`/`disable` fields
- [x] 3.2 Add schema validation to export — validate flattened result before returning
- [x] 3.3 Add `GET /api/rulesets/{id}/export` endpoint — return flattened JSON with `Content-Disposition: attachment`, 404 if no local component, 422 if validation fails
- [x] 3.4 Register export endpoint in `RuleSetApiEndpoints.MapRuleSetApi()`
- [x] 3.5 Write tests for `RuleSetExporter` — standalone local, merged (community + local), standalone override, override-field stripping, canonical formatting

## 4. UI Changes

- [x] 4.1 Add validation error display to `RuleSetBuilder.vue` — show errors from 422 response near top of form, clear on retry
- [x] 4.2 Add "Export for Community" button to `RuleSetDetail.vue` — visible when `sourceType` is "local" or "merged", triggers file download via export endpoint
- [x] 4.3 Handle export errors in detail page — show 422 validation errors in toast or error panel
