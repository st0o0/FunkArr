## Why

The HTTP API layer has grown organically across 6 endpoint files scattered in 4 namespace folders (Indexer, DownloadClient, RuleSet, Configuration). Auth logic is duplicated in 4 near-identical filter implementations, DI parameters repeat on every handler, anonymous response objects block OpenAPI documentation, and there is no API versioning or documentation UI. This change consolidates the API surface, introduces proper structure, and lays the foundation for a sustainable API.

## What Changes

- **Consolidate all HTTP endpoint files into a dedicated `Api/` folder** as ASP.NET MVC controllers, replacing the 6 scattered `*Endpoints.cs` static classes
- **Replace 4 duplicate auth filter implementations** (3 copy-paste `IEndpointFilter` classes + 1 inline method) with a single `ApiKeyMiddleware`
- **Add URL-based API versioning** (`/api/v1/...`) for all Web UI endpoints; protocol emulation endpoints (Newznab, SABnzbd) remain unversioned as their routes are dictated by external specs — **BREAKING** for Web UI API consumers
- **Introduce typed request/response models** in `Api/Models/` replacing anonymous objects, enabling OpenAPI schema generation
- **Integrate Scalar** as interactive API documentation UI at `/scalar`, backed by .NET built-in OpenAPI support
- **Extract shared `PathMappingHelper`** from duplicated code in Queue and SABnzbd endpoints
- **Update Vue frontend** `client.ts` base path from `/api/` to `/api/v1/`

## Capabilities

### New Capabilities

- `api-versioning`: URL-segment API versioning infrastructure with Asp.Versioning.Mvc, version-neutral support for protocol emulation controllers
- `api-documentation`: Scalar UI integration with OpenAPI spec generation, controller tagging, and typed response metadata
- `api-key-middleware`: Centralized API key authentication middleware replacing scattered filter implementations, with route-aware response format (XML for Newznab, JSON for everything else)

### Modified Capabilities

- `newznab-indexer`: Migrated from Minimal API endpoint to MVC controller, marked version-neutral, tagged for OpenAPI grouping
- `sabnzbd-download-client`: Migrated from Minimal API endpoint to MVC controller, marked version-neutral, inline auth removed (handled by middleware)
- `queue-api`: Migrated to versioned controller at `/api/v1/queue` and `/api/v1/history`
- `ruleset-api`: Migrated to versioned controller at `/api/v1/rulesets`
- `match-intelligence-api`: Migrated to versioned controller at `/api/v1/matches`
- `config-api`: Migrated to versioned controller at `/api/v1/config`
- `setup-wizard`: Setup test endpoints migrated to versioned controller at `/api/v1/setup`
- `settings-view`: Frontend API paths updated to `/api/v1/` prefix

## Impact

- **API routes**: All Web UI JSON endpoints move from `/api/*` to `/api/v1/*` — breaking change for any external consumers (the Vue UI is updated in this change)
- **NuGet dependencies**: Adds `Asp.Versioning.Mvc`, `Asp.Versioning.Mvc.ApiExplorer`, `Scalar.AspNetCore`
- **Project structure**: New `src/FunkArr/Api/` and `src/FunkArr/Api/Models/` folders; 6 old endpoint files + 1 filter file deleted
- **Startup pipeline**: `FunkArrServiceSetup` adds `AddControllers()` + versioning + OpenAPI; `FunkArrApplicationSetup` replaces 6 `Map*Endpoints()` calls with `UseMiddleware<ApiKeyMiddleware>()` + `MapControllers()`
- **Tests**: Integration endpoint tests need route updates from `/api/*` to `/api/v1/*`; auth filter unit tests replaced by middleware tests
