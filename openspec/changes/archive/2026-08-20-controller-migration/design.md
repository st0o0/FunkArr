## Context

FunkArr exposes 24 HTTP endpoints across 6 static endpoint classes scattered in 4 namespace folders. Auth is duplicated in 4 near-identical implementations. All handlers use anonymous response objects, preventing OpenAPI documentation. There is no API versioning or documentation UI. The current Minimal API pattern works but creates friction as the API surface grows.

The project uses the Servus AppBuilder startup pattern with three setup containers: `FunkArrServiceSetup` (DI), `FunkArrActorSystemSetup` (actors), `FunkArrApplicationSetup` (pipeline + endpoints).

## Goals / Non-Goals

**Goals:**
- Consolidate all HTTP-facing code into `src/FunkArr/Api/` as MVC controllers
- Single auth middleware replacing 4 duplicate filter implementations
- URL-based API versioning (`/api/v1/`) for Web UI endpoints
- Typed request/response DTOs enabling OpenAPI schema generation
- Scalar UI at `/scalar` for interactive API documentation
- Eliminate code duplication (auth filters, path mapping helpers)

**Non-Goals:**
- Changing domain logic, actor hierarchy, or business behavior
- Header-based or query-string versioning (URL segment is simplest and most visible)
- Authentication/authorization beyond API key (no OAuth, no JWT)
- Rate limiting or API throttling
- Generating client SDKs from OpenAPI spec

## Decisions

### 1. MVC Controllers over Minimal API

**Choice:** Migrate all 6 endpoint files to `[ApiController]` classes inheriting `ControllerBase`.

**Why:** The current static classes with private static handlers already mimic controller structure but without the benefits — constructor injection, model binding attributes, `[ProducesResponseType]`, route attributes. Controllers formalize what's already happening and reduce per-handler ceremony.

**Alternative considered:** Keep Minimal API but reorganize into `Api/` folder. Rejected because Minimal API still requires manual parameter injection per handler and doesn't integrate as cleanly with OpenAPI metadata attributes.

### 2. Middleware over Action Filters for Auth

**Choice:** Single `ApiKeyMiddleware` in the ASP.NET pipeline, not `IActionFilter` or `IAuthorizationFilter`.

**Why:** Auth needs to run for ALL requests to protected routes, not just controller actions. Middleware runs before routing/controller selection, can short-circuit early, and the route-aware response format logic (XML for Newznab vs JSON for everything else) is cleanly expressed as path matching. A filter would need to be applied to every controller individually or via a global convention.

**Alternative considered:** Single `IActionFilter` registered globally. Works but requires controller-level opt-out for unprotected routes and doesn't short-circuit as early in the pipeline.

**Route matching logic:**
```
Skip auth:     /healthz, /alive, /api/fake_nzb, /scalar/*, /openapi/*, static files
XML error:     path == "/api" (bare Newznab root)  
JSON 401:      /api/v1/*, /download/api*
Pass through:  everything else (404 will handle unknown routes)
```

### 3. URL-Segment Versioning

**Choice:** `/api/v1/...` using `Asp.Versioning.Mvc` with `UrlSegmentApiVersionReader`.

**Why:** Most visible versioning scheme, works naturally with route attributes, no ambiguity about which version a request targets. Protocol emulation routes (Newznab at `/api`, SABnzbd at `/download/api`) are marked `[ApiVersionNeutral]` since their shape is dictated by external specs.

**Alternative considered:** Query string versioning (`?api-version=1`). Would conflict with the existing `?apikey=` pattern and is less discoverable.

### 4. Folder Structure

**Choice:**
```
src/FunkArr/Api/
├── Controllers/
│   ├── NewznabController.cs
│   ├── SabnzbdController.cs
│   ├── QueueController.cs
│   ├── RulesetController.cs
│   ├── MatchIntelligenceController.cs
│   └── SetupController.cs
├── Models/
│   ├── ErrorResponse.cs
│   ├── QueueResponses.cs
│   ├── RulesetResponses.cs
│   ├── SetupResponses.cs
│   └── MatchResponses.cs
├── ApiKeyMiddleware.cs
└── PathMappingHelper.cs
```

**Why:** `Api/` as the top folder (not `Controllers/`) keeps all HTTP-facing code together including middleware, models, and shared helpers. The `Controllers/` subfolder holds the actual controller classes. This matches the convention-over-configuration expectations of `AddControllers()` while keeping the API layer self-contained.

### 5. Typed Response Models

**Choice:** Sealed record DTOs in `Api/Models/` replacing anonymous objects. Shared `ErrorResponse` record for consistent error format.

**Why:** Anonymous objects (`new { success = true, error = "..." }`) prevent OpenAPI schema generation. Typed records give us: compile-time safety, OpenAPI schemas, `[ProducesResponseType]` support, and IDE discoverability. Using records keeps them concise.

**Pattern:**
```csharp
// ActionResult<T> for typed responses
[ProducesResponseType<QueueItemResponse[]>(200)]
[ProducesResponseType<ErrorResponse>(401)]
public async Task<ActionResult<QueueItemResponse[]>> GetQueue() { ... }
```

### 6. SetupController Combines Two Route Groups

**Choice:** Single `SetupController` handling both `/api/v1/setup/*` and `/api/v1/config/*` routes using explicit `[Route]` on each action.

**Why:** These share the same concern (configuration management) and the same dependencies. Splitting into two controllers would duplicate constructor parameters without adding clarity.

### 7. Startup Pipeline Changes

**Choice:** `FunkArrServiceSetup` adds `services.AddControllers()`, API versioning, and OpenAPI. `FunkArrApplicationSetup` adds `UseMiddleware<ApiKeyMiddleware>()` and `MapControllers()`, replacing the 6 `Map*Endpoints()` calls.

**Pipeline order:**
```
UseStaticFiles()
UseMiddleware<ApiKeyMiddleware>()
MapHealthChecks("/healthz")
MapGet("/alive", ...)
MapControllers()           // replaces 6 Map*Endpoints() calls
MapScalarApiReference()
MapOpenApi()
MapFallbackToFile("index.html")
```

### 8. Frontend Base Path

**Choice:** Add `API_BASE = '/api/v1'` constant in `client.ts`. All Vue components use this constant instead of hardcoded `/api/` paths.

**Why:** Single place to change if the API version changes. The current `api()` helper function already centralizes fetch logic, so this is a natural extension.

## Risks / Trade-offs

- **[Breaking change for external API consumers]** → Web UI routes move from `/api/*` to `/api/v1/*`. Mitigation: the only known consumer is the bundled Vue UI, updated in this change. Document the change in release notes. Protocol emulation routes (Newznab, SABnzbd) are unchanged — Prowlarr/Sonarr/Radarr configs are not affected.

- **[Controller discovery requires convention]** → `AddControllers()` discovers controllers by assembly scanning. If the `Api/Controllers/` folder is not scanned, controllers won't register. Mitigation: controllers are in the same assembly (`FunkArr`), so default scanning works. No extra configuration needed.

- **[Middleware auth runs on all requests]** → The middleware checks every request, including static files. Mitigation: static file requests are served by `UseStaticFiles()` which runs before the middleware in the pipeline. The middleware skips non-API paths via path prefix matching, so overhead is a single string comparison.

- **[NuGet dependency increase]** → Adding 3 packages (Asp.Versioning.Mvc, Asp.Versioning.Mvc.ApiExplorer, Scalar.AspNetCore). Mitigation: all are well-maintained, widely used packages. Asp.Versioning is the official Microsoft API versioning library.

## Open Questions

None — all design decisions resolved during exploration.
