## MODIFIED Requirements

### Requirement: Health and liveness endpoints

`FunkArrApplicationSetup` SHALL map:
- `GET /healthz` -- ASP.NET health checks endpoint (200 for healthy/degraded, 503 for unhealthy)
- `GET /alive` -- simple liveness probe returning 200 with body `"Alive"`
- `app.MapIndexerApi()` -- Newznab indexer API (parameterless, dependencies resolved via DI)
- `app.MapDownloadApi()` -- SABnzbd download client API (parameterless, dependencies resolved via DI)

`ApplicationSetupContainer` SHALL NOT resolve `IActorRegistry`, `IOptions<FunkArrOptions>`, or any `IActorRef` directly. All dependency resolution SHALL happen inside the endpoint handlers.

#### Scenario: Liveness probe responds

- **WHEN** `GET /alive` is requested
- **THEN** the response status is 200 and body is `"Alive"`

#### Scenario: ApplicationSetupContainer has no manual DI resolution

- **WHEN** reviewing `ApplicationSetupContainer.SetupApplication`
- **THEN** the method SHALL NOT call `GetRequiredService<IActorRegistry>()`, `GetRequiredService<IOptions<FunkArrOptions>>()`, or `registry.Get<T>()`
