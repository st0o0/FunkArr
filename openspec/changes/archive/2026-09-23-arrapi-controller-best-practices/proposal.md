## Why

ArrApi services (`NewznabSearchService`, `SabnzbdQueueService`, `SabnzbdDownloadService`, `NzbService`) return `IActionResult` — an ASP.NET MVC type — coupling protocol-translation logic to the HTTP framework. This prevents testing services without MVC infrastructure, violates Microsoft's controller-based API best practices (services should return domain types, controllers map to HTTP), and makes the code harder to reason about. Controllers also use `new JsonResult(...)` instead of `ControllerBase` helper methods.

## What Changes

- Introduce domain result types for each service method (replacing `IActionResult` returns)
- Services return domain results; controllers map them to `IActionResult` using `ControllerBase` helpers (`Ok()`, `BadRequest()`, `NotFound()`, etc.)
- Controllers use `[ProducesResponseType]` attributes where applicable
- Replace `new JsonResult(new { ... }) { StatusCode = N }` with typed responses and controller helpers
- SABnzbd/Newznab protocol-specific response formats preserved (no ProblemDetails — external protocols dictate the shape)
- Existing tests adapted to assert on domain result types instead of `IActionResult`

## Capabilities

### New Capabilities

- `arrapi-service-results`: Domain result types returned by ArrApi services, replacing `IActionResult`. Covers result types for Newznab search/NZB operations and SABnzbd queue/download/history operations.

### Modified Capabilities

- `arr-api-structure`: Controllers become responsible for HTTP mapping (service → `IActionResult`). Services lose `Microsoft.AspNetCore.Mvc` dependency. Update terminology from "MVC Controllers" to "controller-based API".

## Impact

- `FunkArr.ArrApi/` — all services and both controllers modified
- `FunkArr.ArrApi.Tests/` — service tests simplified (no MVC type assertions)
- No API behavior change — external clients (Sonarr/Radarr/Prowlarr) see identical responses
- No new dependencies
