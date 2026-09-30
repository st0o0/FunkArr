## Context

A full audit of all three API surfaces (Internal `/api`, Newznab `/index/api`, SABnzbd `/download/api`) revealed systemic gaps: zero DataAnnotation usage, zero `ModelState.IsValid` checks, unhandled exception paths in SABnzbd services, an SSRF vector in setup endpoints, and dead request models that specs already require to be wired.

The project uses ASP.NET Minimal APIs for the internal API and controller-based APIs for the *arr adapters. Both support model binding, but neither uses annotation-based validation.

## Goals / Non-Goals

**Goals:**
- Consistent validation via DataAnnotations across all request models
- Automatic validation enforcement (no manual `ModelState.IsValid` in every handler)
- All exception paths produce well-defined responses (no unhandled 500s)
- SSRF protection on setup endpoints
- Clean up dead code and redundant patterns

**Non-Goals:**
- No FluentValidation or third-party validation library — built-in DataAnnotations are sufficient
- No auth changes to internal API (intentionally open within Docker network)
- No response type refactoring — discriminated union result types are working well
- No new middleware — use endpoint filters (Minimal API) and existing `[ApiController]` behavior (controllers)

## Decisions

### 1. Validation approach: DataAnnotations + `[ApiController]` / Endpoint Filter

**Choice:** Use `System.ComponentModel.DataAnnotations` attributes on request records. For controllers, `[ApiController]` already auto-validates and returns 400 when annotations are present. For Minimal APIs, add a `ValidationEndpointFilter` that validates `[AsParameters]`-bound models.

**Why not FluentValidation:** Adds a NuGet dependency and registration ceremony. The validation rules here are simple (required, range, URL format) — DataAnnotations handle this without overhead. The existing `IRuleSetValidator` handles the complex schema validation for rulesets and stays as-is.

**Why not manual validation everywhere:** Already proven inconsistent — 3 models have zero validation. Annotations are declarative, self-documenting, and enforced automatically.

### 2. Wire dead models with `[AsParameters]`

**Choice:** Wire `MediathekSearchRequest` and `DownloadHistoryRequest` via `[AsParameters]` in their endpoint lambdas. The existing spec (`api-request-models`) already requires this.

**Why not delete:** The models exist, match the parameters exactly, and the spec says to use them. Wiring them also enables annotation-based validation on those endpoints.

### 3. SSRF mitigation: URL validation attribute + runtime check

**Choice:** Add a custom `[SafeUrl]` validation attribute that rejects loopback, link-local, and private-range URLs. Applied to `CreateArrResourceRequest.Url`.

**Why a custom attribute:** `[Url]` only validates format, not destination safety. A custom attribute keeps the check declarative and reusable. The runtime check resolves DNS and validates the resolved IP isn't in a private range.

**Why not allowlisting:** The setup endpoints connect to user-configured Sonarr/Radarr/Prowlarr instances — their addresses aren't known in advance. Blocklisting private ranges is the right approach.

### 4. Error handling: service-level try/catch, not controller-level filter

**Choice:** Wrap `Ask<>` calls in SABnzbd services with try/catch, translating exceptions to `SabnzbdResult.Error`. Same pattern Newznab search already uses (but fix its catch-all to differentiate errors).

**Why not an exception filter on controllers:** The discriminated union result types are the right pattern — exceptions should be translated to results in the service layer, not leaked to controllers. Adding a filter would be a safety net that masks the real fix.

### 5. API key passthrough via HttpContext.Items

**Choice:** Store the validated API key in `HttpContext.Items["ApiKey"]` from `ApiKeyActionFilter`, read it in `NewznabController` instead of re-parsing the query string.

**Why:** Eliminates redundant parsing. The filter already validated the key — the controller should trust the filter's result.

## Risks / Trade-offs

- **[Risk] Validation may reject requests that previously "worked"** → Only if callers sent invalid data that happened to not cause errors. This is correct behavior — undefined input should fail fast, not silently proceed.
- **[Risk] `[AsParameters]` changes Minimal API binding behavior** → Tested: ASP.NET binds `[FromQuery]`-annotated record properties identically to individual lambda params. No behavioral change for valid requests.
- **[Risk] SafeUrl DNS resolution adds latency to setup endpoints** → Setup endpoints are called once during initial configuration, not in hot paths. Acceptable tradeoff for SSRF protection.
- **[Risk] Newznab error differentiation may change XML error messages** → Prowlarr/Sonarr only check the error code, not the message text. Safe to change.
