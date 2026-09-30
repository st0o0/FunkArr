## Context

FunkArr.ArrApi is a thin translation layer exposing two external protocol surfaces: Newznab (XML indexer for Prowlarr/Sonarr/Radarr) and SABnzbd (JSON download client for Sonarr/Radarr). Currently implemented as Minimal API with static extension methods (`MapNewznabApi`, `MapSabnzbdApi`), zero DI registrations, and all logic inlined in endpoint lambdas. Two god-files mix routing, serialization, actor communication, response formatting, and NZB parsing.

The project references `FunkArr.Core` (shared types, Akka markers) and delegates domain operations to actors via `IActorRegistry`. The host wires it in `DownloadSetupContainer` with direct calls to the static Map methods.

## Goals / Non-Goals

**Goals:**
- Controllers with constructor-injected services as the routing layer
- Extracted service classes handling protocol translation (actor Ask → response mapping)
- Proper DI registration via `AddArrApiServices()` extension method
- Configurable timeouts and cache TTL via `ArrApiOptions`
- Fix confirmed double-paging bug, dead code, and incomplete category mapping
- Testable services (constructor injection, no static methods with inline dependencies)

**Non-Goals:**
- Changing the Newznab or SABnzbd protocol behavior (same external API contract)
- Modifying actors, messages, or domain projects
- Adding new API endpoints or features
- Introducing interfaces for internal services (concrete classes are fine at v0.x)
- Adding integration tests (existing unit tests updated, new ones for extracted services)

## Decisions

### Decision 1: ASP.NET Controllers over Minimal API

**Choice**: `[ApiController]`-attributed controllers with constructor injection.

**Why**: Each protocol surface (Newznab, SABnzbd) is a cohesive group with shared dependencies (actor refs, options, services). A controller naturally groups these — one class per protocol with deps injected once in the constructor. With Minimal API, each endpoint lambda must independently resolve or receive its dependencies.

**Alternatives considered**:
- *Minimal API + MapGroup + handler classes*: Effectively controllers without the attribute — same structure but more manual wiring. No advantage.
- *Minimal API + services only*: Keeps the current style but extracts services. Still has the "each lambda resolves its own deps" problem.

**Impact**: Requires `AddControllers()` in service registration and `MapControllers()` in app pipeline. The ArrApi project already references `Microsoft.AspNetCore.App` framework.

### Decision 2: Service layer between controllers and actors

**Choice**: Three services for Newznab (`NewznabSearchService`, `NzbService`) and two for SABnzbd (`SabnzbdQueueService`, `SabnzbdDownloadService`), plus a `SabnzbdResponseMapper` static helper.

**Why**: Controllers should be thin dispatchers. The protocol translation logic (building RSS XML, mapping queue items to SABnzbd slots, parsing NZB files) is the real work and belongs in testable services. Services receive actor refs and options via DI.

**Service boundaries**:
- `NewznabSearchService`: Owns `SearchResultCache`, handles search → actor Ask → RSS response. Replaces `SearchHandler` + search-related code from `NewznabApiEndpoints`.
- `NzbService`: NZB get (decode base64 GUID → build NZB XML) and NZB parse (XML → metadata extraction). Replaces inline `NzbGetResult` + `Nzb.cs` parsing logic.
- `SabnzbdQueueService`: Queue, history, fullstatus → actor Ask → JSON response objects. Replaces `QueueResult`, `HistoryResult`, `FullStatusResult` static methods.
- `SabnzbdDownloadService`: AddFile, delete, retry → actor commands. Replaces `DeleteFromQueueResult`, `RetryResult`, addfile POST handler.
- `SabnzbdResponseMapper`: Pure static helper for `QueueSlot`/`HistorySlot` construction, `FormatSpeed`, `FormatTimeLeft`, `MapMediaTypeToCategory`. No DI needed.

### Decision 3: ApiKeyEndpointFilter → IActionFilter

**Choice**: Convert `ApiKeyEndpointFilter` from `IEndpointFilter` to `IAsyncActionFilter` with constructor-injected `IOptions<FunkArrOptions>`.

**Why**: Controllers use MVC filters, not endpoint filters. The filter needs the API key from config — constructor injection is cleaner than service location. The filter still needs per-controller error format (XML for Newznab, JSON for SABnzbd), solved via a constructor parameter or attribute-based configuration.

**Approach**: `ApiKeyActionFilter` takes `IOptions<FunkArrOptions>` via DI. Error format selection via `[ServiceFilter]` with a typed factory, or two thin subclasses (`NewznabApiKeyFilter`, `SabnzbdApiKeyFilter`). Two thin subclasses is simpler — each overrides the error factory method.

### Decision 4: XML response handling

**Choice**: Custom `NewznabXmlResult : IActionResult` that serializes to UTF-8 XML with Newznab namespaces.

**Why**: Controllers return `IActionResult`. The existing `XmlResult` / `Serialize` helper needs to become a proper action result. A custom `IActionResult` encapsulates the XML serialization with proper content type and encoding.

### Decision 5: ArrApiOptions for configurable values

**Choice**: `ArrApiOptions` record with `SearchTimeoutSeconds` (default 30), `DownloadTimeoutSeconds` (default 10), `SearchCacheTtlSeconds` (default 60).

**Why**: Currently hardcoded as `TimeSpan.FromSeconds(30)`, `TimeSpan.FromSeconds(10)`, `TimeSpan.FromSeconds(60)` across files. A single options class makes them configurable and testable.

**Binding**: `configuration.GetSection("FunkArr:ArrApi")`, registered in `AddArrApiServices()`.

### Decision 6: Fix double-paging bug

**Choice**: Paging happens once in the service layer. The RSS mapper (`ToRss`) receives already-paged items and maps without skip/take.

**Current bug**: `Handle()` does `allItems.Skip(offset).Take(limit)` → creates `SearchCommandCompleted` with paged items → `ToRss()` does `completed.Items.Skip(offset).Take(limit)` again. With non-zero offset, results are wrong.

**Fix**: `NewznabSearchService.Search()` pages the cached results, then passes the paged slice + total count to the RSS builder. The RSS builder only maps items, no paging.

## Risks / Trade-offs

- **[Risk] AddControllers() adds MVC infrastructure** → Mitigated: the project already targets ASP.NET Core. The overhead is minimal — no views, no Razor, just controller discovery. Other API surfaces (FunkArr.Api) already use Minimal API and won't be affected.

- **[Risk] Architecture tests may need updating** → ArchUnitNET tests in `FunkArr.Architecture.Tests` may have rules about endpoint classes or naming conventions. Check and update if needed.

- **[Trade-off] Two filter subclasses instead of one generic filter** → Slightly more code, but avoids the complexity of a generic filter factory. Each filter is 5-10 lines.

- **[Trade-off] Nzb.cs moves from root to Newznab/** → Both Newznab (generation) and SABnzbd (parsing) use Nzb. Keeping it in `Newznab/` is acceptable because the model is fundamentally a Newznab concept (NZB format), and SABnzbd just consumes it. The `NzbService` in `Newznab/` exposes both operations; `SabnzbdDownloadService` depends on `NzbService` for parsing.
