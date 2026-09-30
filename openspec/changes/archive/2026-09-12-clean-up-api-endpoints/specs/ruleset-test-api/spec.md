## REMOVED Requirements

### Requirement: Separate test endpoint file
**Reason**: Test endpoint is merged into `RuleSetApiEndpoints` as part of the single-file-per-route-group consolidation. Parsing logic moves to `RuleSetTestRequestParser`.
**Migration**: The `POST /api/rulesets/test` route remains identical. `RuleSetTestApiEndpoints` is deleted — the endpoint registration moves into `RuleSetApiEndpoints.MapRuleSetApi()` and parsing logic moves to `RuleSetTestRequestParser`.
