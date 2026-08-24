## Purpose

OpenAPI spec-first contract generation for FunkArr's internal REST API. Modular OpenAPI 3.1 specs are maintained under `openapi/`, NSwag generates C# contract types at build time, and extension methods map between domain and contract types. SABnzbd and Newznab external protocol types remain hand-written.

## Requirements

### Requirement: Modular OpenAPI spec files
The system SHALL maintain OpenAPI 3.1 specification files under `openapi/` in the repository root. The specs SHALL be structured as one root file (`funkArr-v1.yaml`) composing per-feature files via `$ref`: `queue.yaml`, `setup.yaml`, `rulesets.yaml`, `match-intelligence.yaml`.

#### Scenario: Root spec composes feature files
- **WHEN** the root spec `openapi/funkArr-v1.yaml` is loaded
- **THEN** it SHALL contain `info`, `servers`, `security` definitions and `$ref` paths to all feature-specific YAML files

#### Scenario: Feature spec is self-contained
- **WHEN** a feature spec (e.g. `openapi/queue.yaml`) is examined
- **THEN** it SHALL define all paths and component schemas for that feature area

#### Scenario: Specs cover only internal REST API
- **WHEN** listing all paths across all OpenAPI spec files
- **THEN** no SABnzbd (`/download/api`) or Newznab (`/api`) paths SHALL be present — those implement external protocols

### Requirement: NSwag code generation
The system SHALL use NSwag to generate C# contract types from the OpenAPI specs as a MSBuild pre-build step. Generated types SHALL be placed in `Api/Generated/Contracts.g.cs` with namespace `FunkArr.Api.Contracts`.

#### Scenario: Generated file produced by build
- **WHEN** `dotnet build` is run and OpenAPI specs have changed
- **THEN** NSwag SHALL regenerate `Api/Generated/Contracts.g.cs` with updated types

#### Scenario: Generated types are records
- **WHEN** NSwag generates a schema type (e.g. `QueueItem` from the queue spec)
- **THEN** the generated C# type SHALL be a record with nullable annotations enabled

#### Scenario: Generated file is checked into git
- **WHEN** the generated file is produced
- **THEN** it SHALL be committed to git so CI builds do not require NSwag tooling

### Requirement: Extension-method-based contract mapping
The system SHALL provide extension methods in `Api/Mapping/ContractMappingExtensions.cs` for converting between domain types and generated contract types. Mapping methods SHALL follow the pattern `domain.ToContract()`.

#### Scenario: Domain to contract mapping
- **WHEN** a controller needs to return a contract type
- **THEN** it SHALL call `domainObject.ToContract()` to produce the generated contract type

#### Scenario: Contract to domain mapping for request bodies
- **WHEN** a controller receives a generated contract type as request body
- **THEN** it SHALL call `contractObject.ToDomain()` to produce the domain type for actor messaging

### Requirement: SABnzbd responses stay hand-written
The SABnzbd response types SHALL remain hand-written in `Api/Contracts/Sabnzbd/SabnzbdResponses.cs`. They SHALL NOT be generated from OpenAPI specs.

#### Scenario: SABnzbd types use JsonPropertyName
- **WHEN** examining SABnzbd response types
- **THEN** they SHALL use `[JsonPropertyName]` attributes with SABnzbd-compatible snake_case keys

#### Scenario: SABnzbd types relocate from Api/Models
- **WHEN** the `Api/Models/` directory is removed
- **THEN** SABnzbd response types SHALL be found in `Api/Contracts/Sabnzbd/`

### Requirement: Controllers use generated contract types
All internal REST API controllers (QueueController, SetupController, RulesetController, MatchIntelligenceController) SHALL return generated contract types from `FunkArr.Api.Contracts`, not domain types. Domain-to-contract conversion SHALL use extension methods.

#### Scenario: No domain type in ProducesResponseType
- **WHEN** examining `[ProducesResponseType]` attributes on internal REST API controllers
- **THEN** all referenced types SHALL be from `FunkArr.Api.Contracts` namespace (or `FunkArr.Api.Contracts.Sabnzbd` for SABnzbd)

#### Scenario: No domain using directive in controllers
- **WHEN** examining using directives in internal REST API controllers
- **THEN** no controller SHALL import domain namespaces (`FunkArr.RuleSet`, `FunkArr.Shared.Models`) for the purpose of return types — only for actor messaging

### Requirement: Api/Models directory removed
The `Api/Models/` directory SHALL be deleted. All types previously in `Api/Models/` SHALL either be replaced by generated contract types or relocated to `Api/Contracts/`.

#### Scenario: No Api/Models directory exists
- **WHEN** examining the project folder structure after this change
- **THEN** no `Api/Models/` directory SHALL exist

#### Scenario: ErrorResponse relocated
- **WHEN** controllers need to return error responses
- **THEN** they SHALL use `ErrorResponse` from `FunkArr.Api.Contracts`
