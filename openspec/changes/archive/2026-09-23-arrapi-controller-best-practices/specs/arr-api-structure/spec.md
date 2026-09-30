## MODIFIED Requirements

### Requirement: Unified ArrApi project
`FunkArr.ArrApi` SHALL be a single adapter project containing both the Newznab indexer API and SABnzbd download client API. It SHALL use ASP.NET Core controller-based API pattern (`ControllerBase` + `[ApiController]`) with constructor-injected services instead of Minimal API static extension methods. Services SHALL be registered via an `AddArrApiServices()` IServiceCollection extension method. Services SHALL return domain result types; controllers SHALL own HTTP-response mapping.

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
