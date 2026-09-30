## REMOVED Requirements

### Requirement: RuleSetGenerationService
**Reason**: The `*Service` suffix is banned. Generation logic already lives in Core (`RuleSetGenerator`). The orchestration (fetch from gateway + generate + return preview) moves to a controller helper or actor message handler.
**Migration**: Callers that used `RuleSetGenerationService.GenerateShowPreview()` / `GenerateMoviePreview()` SHALL call gateway actors directly and then invoke `RuleSetGenerator` from Core. The preview API endpoints remain unchanged but their implementation no longer routes through a service class.
