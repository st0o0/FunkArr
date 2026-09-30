## MODIFIED Requirements

### Requirement: Health and liveness endpoints
`FunkArrApplicationSetup` SHALL map:
- `GET /healthz` -- ASP.NET health checks endpoint (200 for healthy/degraded, 503 for unhealthy)
- `GET /alive` -- simple liveness probe returning 200 with body `"Alive"`
- `app.UseStaticFiles()` -- serve Vue frontend assets from dist/
- `app.MapRuleSetApi()` -- internal REST API for rulesets and scoring history
- `app.MapSetupApi()` -- internal REST API for setup health checks
- `app.MapIndexerApi()` -- Newznab indexer API (parameterless, dependencies resolved via DI)
- `app.MapDownloadApi()` -- SABnzbd download client API (parameterless, dependencies resolved via DI)
- SPA fallback route serving `index.html` for unmatched routes (mapped last)

`ApplicationSetupContainer` SHALL NOT resolve `IActorRegistry`, `IOptions<FunkArrOptions>`, or any `IActorRef` directly. All dependency resolution SHALL happen inside the endpoint handlers.

#### Scenario: Liveness probe responds
- **WHEN** `GET /alive` is requested
- **THEN** the response status is 200 and body is `"Alive"`

#### Scenario: ApplicationSetupContainer has no manual DI resolution
- **WHEN** reviewing `ApplicationSetupContainer.SetupApplication`
- **THEN** the method SHALL NOT call `GetRequiredService<IActorRegistry>()`, `GetRequiredService<IOptions<FunkArrOptions>>()`, or `registry.Get<T>()`

#### Scenario: Health check responds when healthy
- **WHEN** `GET /healthz` is requested and all health checks pass
- **THEN** the response status is 200

#### Scenario: Setup API is mapped
- **WHEN** `GET /api/health/setup` is requested
- **THEN** the setup health check endpoint responds (not 404)

#### Scenario: Static files and SPA fallback are mapped
- **WHEN** reviewing `ApplicationSetupContainer.SetupApplication`
- **THEN** `UseStaticFiles()` is called before endpoint mapping, `MapRuleSetApi()` and `MapSetupApi()` are called, and a SPA fallback route is registered last

#### Scenario: SPA fallback does not intercept API routes
- **WHEN** `GET /api/rulesets` is requested
- **THEN** the API endpoint responds, not the SPA fallback
