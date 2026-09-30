## Context

ArrApi is a thin protocol-translation layer — it converts Newznab XML and SABnzbd JSON protocols into Akka actor messages and back. After the recent migration from Minimal APIs to controller-based API (`ControllerBase` + `[ApiController]`), the services still return `IActionResult`, coupling them to the MVC framework unnecessarily.

Microsoft's best practices for controller-based APIs are clear: services return domain types, controllers handle HTTP mapping. The current code puts HTTP concerns (status codes, `JsonResult`, `ContentResult`) deep into services.

## Goals / Non-Goals

**Goals:**
- Services return domain result types, not `IActionResult`
- Controllers own all HTTP-response mapping using `ControllerBase` helpers
- Services have no dependency on `Microsoft.AspNetCore.Mvc` types
- Service tests assert on domain types, not MVC result types
- Update `arr-api-structure` spec terminology from "MVC" to "controller-based API"

**Non-Goals:**
- Changing the external API contract (Sonarr/Radarr/Prowlarr must see identical responses)
- Introducing `ProblemDetails` for errors (SABnzbd and Newznab define their own error formats)
- Splitting the single-endpoint switch dispatch (protocol-mandated)
- Adding `ActionResult<T>` typed returns (the response shapes are protocol-specific, not suitable for generic OpenAPI inference)
- Adding `[ProducesResponseType]` (these are external protocol endpoints, not OpenAPI-first)

## Decisions

### Decision 1: Result types as sealed records in each namespace

Result types live alongside the service that returns them, not in a shared file.

- `FunkArr.ArrApi.Newznab`: `SearchServiceResult`, `NzbGetResult`
- `FunkArr.ArrApi.Sabnzbd`: `SabnzbdResult` (shared for all SABnzbd operations)

**Why records**: Immutable, pattern-matchable, consistent with the project's conventions.

**Why per-namespace, not per-method**: The SABnzbd operations share a common `{status, error}` success/fail shape. One `SabnzbdResult` discriminated union covers most methods. Newznab has two distinct shapes (XML RSS vs NZB file download), so two result types.

### Decision 2: Discriminated union style with abstract + sealed subtypes

```csharp
public abstract record SabnzbdResult
{
    public sealed record Success(object Data) : SabnzbdResult;
    public sealed record Error(string Message, int StatusCode = 400) : SabnzbdResult;
}
```

This matches the project's existing message convention (e.g., `SearchCommandResponse` with `Completed`/`Failed` subtypes) and enables exhaustive switch in controllers.

**Alternative considered**: `OneOf<T1, T2>` — rejected because it adds a NuGet dependency for a simple pattern the project already uses natively.

### Decision 3: Controllers use ControllerBase helpers where protocol allows

- SABnzbd JSON: `Ok(data)`, `BadRequest(error)`, `StatusCode(502, msg)` — these produce the same JSON shapes
- Newznab XML: Keep `NewznabXmlResult` custom `IActionResult` — XML serialization requires custom handling, `ControllerBase` has no XML helper

### Decision 4: NewznabXmlResult stays as IActionResult

`NewznabXmlResult` is a custom `IActionResult` that handles XML serialization. This is the correct pattern for custom content types in controller-based APIs. It does NOT move into services — it stays as the controller's mapping mechanism.

However, the `Error()` static method that returns `ContentResult` should also stay at the controller level, called when the service returns an error result.

### Decision 5: NzbService split — domain logic vs HTTP mapping

`NzbService.GetNzb()` currently returns `IActionResult` mixing validation, Base64 decoding, and `FileContentResult` construction. Split into:
- Service: validates, decodes, returns `NzbGetResult` (the raw bytes + filename, or an error)
- Controller: maps to `FileContentResult` or `NewznabXmlResult.Error()`

`NzbService.ParseNzb()` already returns a domain type (`NzbParseResult?`) — no change needed.

## Risks / Trade-offs

- **[More code in controllers]** → Controllers grow from 1-2 lines to a switch per service call. Acceptable trade-off: controllers are meant to be HTTP-mapping glue.
- **[Churn in tests]** → Service tests change from asserting `IActionResult` subtypes to asserting domain record types. This actually simplifies them (no casting to `JsonResult` to inspect `Value`).
- **[SABnzbd result type carries `object Data`]** → The SABnzbd protocol returns various anonymous JSON shapes. Using `object` is pragmatic — the shapes are protocol-defined and never deserialized by us. Alternative: one result type per operation — rejected as over-engineering for a thin adapter.
