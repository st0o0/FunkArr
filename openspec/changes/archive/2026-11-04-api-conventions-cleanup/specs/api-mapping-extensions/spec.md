## ADDED Requirements

### Requirement: FeedMappingExtensions file
A `FeedMappingExtensions.cs` file SHALL exist in `Extensions/` containing `ToApi()` methods for feed response mapping. The `FeedResult`/`FeedCrawlStatusResult` → `FeedOverviewResponse` mapping that currently lives in `FeedApiEndpoints.ToApi()` SHALL move here.

#### Scenario: Feed mapping in extensions
- **WHEN** `Extensions/FeedMappingExtensions.cs` is inspected
- **THEN** it SHALL contain a `ToApi()` extension method that maps `FeedResult` + `FeedCrawlStatusResult` to `FeedOverviewResponse`

#### Scenario: No mapping in FeedApiEndpoints
- **WHEN** `FeedApiEndpoints.cs` is inspected
- **THEN** no `ToApi` or response-construction logic SHALL exist in the file

### Requirement: SystemMappingExtensions file
A `SystemMappingExtensions.cs` file SHALL exist in `Extensions/` containing `ToApi()` methods for system response mapping. The variant policy mapping (`ToVariantsResponse`, `ToApi(VariantPolicy)`) that currently lives in `SystemApiEndpoints` SHALL move here.

#### Scenario: Variant mapping in extensions
- **WHEN** `Extensions/SystemMappingExtensions.cs` is inspected
- **THEN** it SHALL contain methods to map `VariantOptions` to `VariantsResponse`

#### Scenario: No mapping in SystemApiEndpoints
- **WHEN** `SystemApiEndpoints.cs` is inspected
- **THEN** no `ToVariantsResponse` or `ToApi(VariantPolicy)` methods SHALL exist

### Requirement: MediaType.ToApi extension for feed entries
`FeedEntry.MediaType` (domain `Messages.MediaType`) SHALL be mapped to `Api.Models.Shared.MediaType` via an exhaustive switch extension method, not inline in the endpoint.

#### Scenario: Feed entry media type mapped via extension
- **WHEN** a feed entry with `MediaType.Show` is mapped
- **THEN** the extension method SHALL return `Api.Models.Shared.MediaType.Show`

## MODIFIED Requirements

### Requirement: Endpoint classes are thin
Endpoint classes SHALL contain only inline lambda route definitions, parameter binding, and actor communication. All model mapping SHALL use `ToApi()`/`ToMessage()`/`ToCommand()` extension methods from `Extensions/`. No private/internal static mapper methods or handler methods SHALL exist in endpoint classes. The only exception is `SystemApiEndpoints` which may contain health probe utility methods (not endpoint handlers).

#### Scenario: RuleSetApiEndpoints has no helper methods
- **WHEN** `RuleSetApiEndpoints.cs` is inspected
- **THEN** it SHALL contain no `private static` methods that serve as endpoint handlers or mappers
- **AND** all endpoints SHALL be inline lambdas

#### Scenario: DownloadsApiEndpoints has no helper methods
- **WHEN** `DownloadsApiEndpoints.cs` is inspected
- **THEN** it SHALL contain no `ParseMediaType` or other helper methods

#### Scenario: SystemApiEndpoints retains health probes
- **WHEN** `SystemApiEndpoints.cs` is inspected
- **THEN** health check probe methods (`CheckApiKey`, `CheckMediathekViewWeb`, etc.) MAY exist
- **AND** endpoint handlers SHALL still be inline lambdas
