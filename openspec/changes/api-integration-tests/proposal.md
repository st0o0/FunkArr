## Why

FunkArr has 800+ unit tests but zero integration tests for the HTTP layer. API endpoints (35 in FunkArr.Api, plus Newznab/SABnzbd controllers in FunkArr.ArrApi) are only tested at the mapping/serialization level. No test ever sends a real HTTP request through the routing pipeline, validation filters, API key checks, or content negotiation.

`Microsoft.AspNetCore.Mvc.Testing` is already pinned in `Directory.Packages.props` but not referenced by any test project.

## What Changes

Build a custom `FunkArrTestServer` that boots a minimal ASP.NET host with TestProbe-backed actors, then write integration tests that send real HTTP requests and assert on status codes, response bodies, headers, and content types.

### Approach: Custom TestServer (not WebApplicationFactory\<Program\>)

`WebApplicationFactory<Program>` would boot the full Servus AppBuilder including Akka Clustering, Remoting (port 2552), and Persistence. That brings port conflicts, slow startup, flaky shard joins, and makes actor behavior hard to control. Instead:

- Build a `FunkArrTestServer` helper that creates a `WebApplication` from scratch
- Register only the services the API layer needs: Options, DataPaths, IDataFiles, IActorRegistry, Controllers
- Start a minimal ActorSystem (no clustering, no remoting, no persistence)
- Register TestProbes under each actor key via `ActorRegistry.Register<TKey>(probe)`
- Map the same endpoints (MapSystemApi, MapDownloadsApi, etc.) and controllers
- Tests get an `HttpClient` and access to the TestProbes to control actor replies

### What gets tested

**FunkArr.Api (Minimal APIs):**
- Download queue/history endpoints: routing, Ask/response mapping, pagination
- RuleSet CRUD endpoints: routing, validation, response shapes
- System endpoints: health, version, storage, cache
- Mediathek search endpoint

**FunkArr.ArrApi (Controllers):**
- Newznab search (`/index/api?t=tvsearch`): API key filter, XML response format, caps response
- SABnzbd download client (`/download/api`): API key filter, queue/history/version modes
- API key rejection (missing key, wrong key -> proper error responses)

### What does NOT get tested here

- Actor behavior (already covered by unit tests with TestKit)
- Persistence (already covered by actor tests)
- Full end-to-end flows (search -> score -> download) -- deferred to future Option B smoke tests

## Capabilities

### New Capabilities

- `test-server`: `FunkArrTestServer` helper class that boots a minimal ASP.NET host with TestProbe-backed IActorRegistry
- `api-integration-tests`: Integration tests for FunkArr.Api endpoints
- `arrapi-integration-tests`: Integration tests for FunkArr.ArrApi controllers

### Modified Capabilities

_(none)_

## Impact

- **FunkArr.Tests.Shared**: New `FunkArrTestServer` helper class
- **FunkArr.Api.Tests**: Add `Microsoft.AspNetCore.Mvc.Testing` package ref, new integration test files
- **FunkArr.ArrApi.Tests**: Add `Microsoft.AspNetCore.Mvc.Testing` package ref, new integration test files
- Both test projects gain a project reference to `FunkArr` (host) for endpoint registration methods
- No changes to production code
