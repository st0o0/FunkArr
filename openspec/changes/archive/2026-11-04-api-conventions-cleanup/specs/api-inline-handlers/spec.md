## ADDED Requirements

### Requirement: All endpoint handlers are inline lambdas
Endpoint files SHALL contain only inline lambda handlers in `Map*` call chains. No `private static` methods SHALL serve as endpoint handlers (method group references like `group.MapPost("/", HandleCreate)` are prohibited). If handler logic exceeds what fits cleanly in an inline lambda, the logic SHALL move to the domain layer (actor/service), not to an extracted method.

#### Scenario: RuleSetApiEndpoints uses inline lambdas
- **WHEN** `RuleSetApiEndpoints.cs` is inspected
- **THEN** all endpoints (`/`, `/{id}`, `/{id}/history`, `/{id}/history/{requestId}`, `/`, `/{id}`, `/{id}`, `/{id}/raw`, `/{id}/export`, `/test`) SHALL use inline lambdas
- **AND** no `HandleCreate`, `HandleUpdate`, `HandleDelete`, `HandleExport`, `HandleGetRaw` methods SHALL exist

#### Scenario: SetupArrEndpoints uses inline lambdas
- **WHEN** `SetupArrEndpoints.cs` is inspected
- **THEN** all endpoints SHALL use inline lambdas
- **AND** no `ResolveSelfUrl` helper method SHALL exist (logic inlined)

#### Scenario: FeedApiEndpoints uses inline lambdas
- **WHEN** `FeedApiEndpoints.cs` is inspected
- **THEN** all endpoints SHALL use inline lambdas
- **AND** no `ToApi` or `ParseMediaType` helper methods SHALL exist (mappings in extensions, parse eliminated by enum binding)

#### Scenario: DownloadsApiEndpoints uses inline lambdas
- **WHEN** `DownloadsApiEndpoints.cs` is inspected
- **THEN** all endpoints SHALL use inline lambdas
- **AND** no `ParseMediaType` helper method SHALL exist

### Requirement: Endpoint lambda shape
Every endpoint handler lambda SHALL follow the pattern: (1) resolve actor(s) from `IActorRegistry`, (2) Ask with a message, (3) switch on response to produce `IResult`. No business logic, multi-actor orchestration, or domain computation SHALL occur in the lambda body.

#### Scenario: Simple GET endpoint
- **WHEN** `GET /api/downloads/queue` handler is inspected
- **THEN** it SHALL resolve `IDownloadManager`, Ask with `QueryQueue`, switch on response

#### Scenario: POST with request mapping
- **WHEN** `POST /api/rulesets` handler is inspected
- **THEN** it SHALL resolve `IRuleSetRegion`, call `request.ToCommand()`, Ask, switch on response, and call cache eviction inline

### Requirement: Test enrichment logic in domain layer
The `RunTestEnrichment`, `EnrichEpisodes`, and `EnrichMovies` methods SHALL be removed from `RuleSetApiEndpoints`. The enrichment orchestration (scoring + optional TVDB/TMDB enrichment) SHALL be handled by a domain-layer command so the `/test` endpoint remains a single Ask→map→Result lambda.

#### Scenario: Test endpoint is a simple Ask
- **WHEN** `POST /api/rulesets/test` handler is inspected
- **THEN** it SHALL contain a single Ask to a scoring/enrichment actor and a response mapping
- **AND** no `RunTestEnrichment`, `EnrichEpisodes`, or `EnrichMovies` methods SHALL exist in the endpoint file

#### Scenario: Test enrichment still works
- **WHEN** a test score request includes `Enrichment.Enabled = true` with a valid `TvdbId`
- **THEN** the response SHALL contain `EnrichmentTrace` data on matched items

### Requirement: SystemApiEndpoints exception
`SystemApiEndpoints` setup checks (`CheckApiKey`, `CheckMediathekViewWeb`, `CheckDirectory`, `CheckSelfEndpoint`, `CheckFfmpeg`) are exempt from the inline-only rule. These are self-contained health probe utilities, not endpoint handler methods. They SHALL remain as helper methods within the class.

#### Scenario: System setup checks co-located
- **WHEN** `SystemApiEndpoints.cs` is inspected
- **THEN** health check probe methods SHALL exist as `private static` or `internal static` methods
- **AND** the `/setup` endpoint handler SHALL still be an inline lambda that calls these probes

### Requirement: Only mapping extensions as non-inline code
The only non-inline code that endpoint lambdas call (besides `IActorRegistry.GetAsync` and `actor.Ask`) SHALL be `ToApi()`/`ToMessage()`/`ToCommand()` mapping extension methods from `Extensions/`. Stray mapping methods in endpoint files (e.g., `ToVariantsResponse` in SystemApiEndpoints, `ToApi` in FeedApiEndpoints) SHALL move to proper mapping extension files.

#### Scenario: FeedApiEndpoints ToApi moves to extension
- **WHEN** `FeedApiEndpoints.cs` is inspected
- **THEN** no `ToApi` method SHALL exist in the file
- **AND** `Extensions/FeedMappingExtensions.cs` SHALL contain the feed response mapping

#### Scenario: SystemApiEndpoints variant mapping moves to extension
- **WHEN** `SystemApiEndpoints.cs` is inspected
- **THEN** no `ToVariantsResponse` or `ToApi(VariantPolicy)` methods SHALL exist
- **AND** variant response mapping SHALL be in a mapping extension file

### Requirement: AGENTS.md documents inline-lambda convention
`AGENTS.md` SHALL contain an API conventions section that documents: all handlers are inline lambdas, the endpoint lambda shape (resolve→Ask→switch), no extracted handler methods, SystemApiEndpoints exception, and mapping extensions as the only non-inline code.

#### Scenario: AGENTS.md has API conventions
- **WHEN** `AGENTS.md` is inspected
- **THEN** it SHALL contain a section covering endpoint handler conventions
