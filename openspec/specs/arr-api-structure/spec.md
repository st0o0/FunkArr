# Capability: Arr API Structure

## Purpose

Defines the unified ArrApi adapter project that consolidates Newznab indexer and SABnzbd download client APIs into a single project with namespace separation.

## Requirements

### Requirement: Unified ArrApi project
`FunkArr.ArrApi` SHALL be a single adapter project containing both the Newznab indexer API and SABnzbd download client API. It SHALL use ASP.NET Core controller-based API pattern (`ControllerBase` + `[ApiController]`) with constructor-injected services instead of Minimal API static extension methods. Services SHALL return domain result types; controllers SHALL own HTTP-response mapping. Services SHALL be registered via an `AddArrApiServices()` IServiceCollection extension method.

#### Scenario: Project exists and compiles
- **WHEN** `dotnet build FunkArr.slnx` is run
- **THEN** `FunkArr.ArrApi` SHALL compile successfully

#### Scenario: Both endpoint groups registered
- **WHEN** the application starts
- **THEN** `/index/api` (Newznab) and `/download/api` (SABnzbd) endpoint groups SHALL both be available via controller routing

#### Scenario: Services registered via extension method
- **WHEN** the host calls `services.AddArrApiServices(configuration)`
- **THEN** `SearchResultCache`, `NewznabSearchService`, `NzbService`, `SabnzbdQueueService`, `SabnzbdDownloadService`, and `ArrApiOptions` SHALL be registered in the DI container

#### Scenario: Controllers discovered
- **WHEN** `services.AddControllers()` is called and `app.MapControllers()` is called
- **THEN** `NewznabController` and `SabnzbdController` SHALL be discovered and mapped

### Requirement: Namespace separation
Newznab-specific types SHALL reside in `FunkArr.ArrApi.Newznab` namespace. SABnzbd-specific types SHALL reside in `FunkArr.ArrApi.Sabnzbd` namespace. Shared types (ApiKeyActionFilter, ArrApiOptions) SHALL reside in `FunkArr.ArrApi` root namespace.

#### Scenario: Newznab types in correct namespace
- **WHEN** examining NewznabController, NewznabSearchService, NzbService, Nzb, Caps, Rss, NewznabError, SearchResultCache
- **THEN** all SHALL be in `FunkArr.ArrApi.Newznab` or `FunkArr.ArrApi.Newznab.Models`

#### Scenario: SABnzbd types in correct namespace
- **WHEN** examining SabnzbdController, SabnzbdQueueService, SabnzbdDownloadService, SabnzbdResponseMapper, QueueResponse, HistoryResponse, FullStatusResponse
- **THEN** all SHALL be in `FunkArr.ArrApi.Sabnzbd` or `FunkArr.ArrApi.Sabnzbd.Models`

#### Scenario: Shared types in root namespace
- **WHEN** examining ApiKeyActionFilter, ArrApiOptions, ServiceCollectionExtensions
- **THEN** all SHALL be in `FunkArr.ArrApi`

### Requirement: Unified ApiKeyEndpointFilter
A single `ApiKeyActionFilter` base class SHALL validate the `apikey` query parameter for both API surfaces using constructor-injected `IOptions<FunkArrOptions>`. Two thin subclasses (`NewznabApiKeyFilter`, `SabnzbdApiKeyFilter`) SHALL produce format-appropriate error responses.

#### Scenario: Newznab error format
- **WHEN** authentication fails on a `/index/api` endpoint
- **THEN** the error response SHALL be Newznab XML: `<error code="100" description="Invalid API Key"/>` with HTTP 403

#### Scenario: SABnzbd error format
- **WHEN** authentication fails on a `/download/api` endpoint
- **THEN** the error response SHALL be JSON: `{"status":false,"error":"API Key Incorrect"}` with HTTP 403

#### Scenario: Valid API key passes through
- **WHEN** a request includes a valid `apikey` parameter
- **THEN** the request SHALL proceed to the controller action regardless of API surface

#### Scenario: API key resolved from options
- **WHEN** the filter validates the API key
- **THEN** it SHALL read the expected key from `IOptions<FunkArrOptions>.Value.ApiKey` via constructor injection, not service location

### Requirement: Co-located NZB generation and parsing
NZB generation and parsing SHALL be encapsulated in `NzbService` within the `FunkArr.ArrApi.Newznab` namespace. The `Nzb` model class SHALL reside in `FunkArr.ArrApi.Newznab` namespace.

#### Scenario: Round-trip integrity
- **WHEN** an NZB is generated with title "Test Show" and url "https://example.com/video.mp4"
- **AND** the generated NZB XML is parsed back
- **THEN** the parsed title SHALL be "Test Show" and the parsed url SHALL be "https://example.com/video.mp4"

#### Scenario: Generator accessible from controller
- **WHEN** NewznabController handles a `t=get` request
- **THEN** it SHALL delegate to `NzbService` for NZB generation

#### Scenario: Parser accessible from download service
- **WHEN** SabnzbdDownloadService handles an `addfile` POST
- **THEN** it SHALL use `NzbService` for NZB parsing

### Requirement: No business logic in adapter
`FunkArr.ArrApi` SHALL NOT contain queue management, history tracking, retry logic, or any other domain state. All stateful operations SHALL be delegated to domain projects via Messages. Controllers SHALL be HTTP-mapping dispatchers that pattern-match on service results. Services SHALL handle protocol translation only and return domain result types without HTTP concerns.

#### Scenario: No DownloadState class
- **WHEN** examining the ArrApi project
- **THEN** no class managing download queue or history state SHALL exist

#### Scenario: Controller methods map service results to HTTP
- **WHEN** examining controller action methods
- **THEN** each method SHALL call a service, pattern-match on the domain result type, and map to `IActionResult` using `ControllerBase` helpers or custom result types

#### Scenario: Services return domain types not IActionResult
- **WHEN** examining service method signatures
- **THEN** no service method SHALL return `IActionResult` or any type from `Microsoft.AspNetCore.Mvc`

### Requirement: ArrApiOptions configuration
`ArrApiOptions` SHALL be a configuration class bound to `FunkArr:ArrApi` section providing configurable timeouts and cache TTL.

#### Scenario: Default values
- **WHEN** no `FunkArr:ArrApi` configuration is provided
- **THEN** `SearchTimeoutSeconds` SHALL default to 30, `DownloadTimeoutSeconds` SHALL default to 10, `SearchCacheTtlSeconds` SHALL default to 60

#### Scenario: Custom values
- **WHEN** `FunkArr__ArrApi__SearchTimeoutSeconds=45` environment variable is set
- **THEN** `ArrApiOptions.SearchTimeoutSeconds` SHALL be 45

#### Scenario: Options registered in DI
- **WHEN** `AddArrApiServices()` is called
- **THEN** `IOptions<ArrApiOptions>` SHALL be available for injection

### Requirement: SABnzbd queue manipulation operations
The SABnzbd API queue mode handler SHALL support `name=priority` and `name=switch` operations in addition to the existing `name=delete`.

#### Scenario: Priority operation
- **WHEN** GET `/download/api?mode=queue&name=priority&value={nzo_id}&value2={priority_int}`
- **THEN** the download's priority SHALL be changed according to the SABnzbd priority mapping

#### Scenario: Switch operation
- **WHEN** GET `/download/api?mode=queue&name=switch&value={nzo_id1}&value2={nzo_id2}`
- **THEN** the two downloads SHALL be swapped

#### Scenario: Unknown operation
- **WHEN** GET `/download/api?mode=queue&name=unknown`
- **THEN** the response SHALL indicate an error

### Requirement: DownloadGetRequest value2 parameter
The `DownloadGetRequest` model SHALL include a `Value2` property bound to the `value2` query parameter for SABnzbd switch and priority operations.

#### Scenario: Value2 binding
- **WHEN** a request contains `&value2=xyz`
- **THEN** `DownloadGetRequest.Value2` SHALL be `"xyz"`
