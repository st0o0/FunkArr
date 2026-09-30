## REMOVED Requirements

### Requirement: Separate write endpoint file
**Reason**: Write endpoints (create, update, delete, get-raw) are merged into `RuleSetApiEndpoints` as part of the single-file-per-route-group consolidation.
**Migration**: All routes remain identical under `/api/rulesets`. Only the file and class structure changes — `RuleSetWriteApiEndpoints` is deleted and its routes move into `RuleSetApiEndpoints.MapRuleSetApi()`.
