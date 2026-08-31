# Capability: Project Structure

## Purpose

Defines all project csproj files in the solution, their SDK types, project references,
and NuGet dependencies. Enforces domain isolation via layered references.

## Requirements

### Requirement: Host project
`FunkArr` SHALL use `Microsoft.NET.Sdk.Web` with `OutputType: Exe`. It SHALL
reference all domain, adapter, and infrastructure projects: Core, Api, ArrApi,
Search, Download, RuleSet, MatchMagic, Messages, Persistence.

#### Scenario: Host references all projects
- **WHEN** the host project is built
- **THEN** it transitively includes all domain and adapter assemblies

### Requirement: Core project
`FunkArr.Core` SHALL use `Microsoft.NET.Sdk` and reference Messages and Persistence.
It SHALL declare NuGet references for Akka.NET (Hosting, Cluster.Sharding, Streams,
Persistence.Sql.Hosting, Logger.Serilog), Servus, and Servus.Akka.

#### Scenario: Domain projects inherit Akka/Servus via Core
- **WHEN** a domain project references Core
- **THEN** Akka.NET and Servus types are available without additional NuGet references

### Requirement: Messages project
`FunkArr.Messages` SHALL use `Microsoft.NET.Sdk` with no project or NuGet references.
It SHALL contain only message types (commands, queries, responses) as sealed records.
It SHALL NOT reference Akka.NET or any domain project.

#### Scenario: Messages has no external dependencies
- **WHEN** the Messages project is built
- **THEN** it has zero NuGet package references and zero project references

### Requirement: Persistence project
`FunkArr.Persistence` SHALL use `Microsoft.NET.Sdk` with no project or NuGet references.
It SHALL contain only persistence DTO types. It SHALL NOT reference Akka.NET or
any domain project.

#### Scenario: Persistence has no external dependencies
- **WHEN** the Persistence project is built
- **THEN** it has zero NuGet package references and zero project references

### Requirement: Domain projects
`FunkArr.Search`, `FunkArr.Download`, `FunkArr.RuleSet`, and `FunkArr.MatchMagic`
SHALL each use `Microsoft.NET.Sdk` and reference only `FunkArr.Core`.
No domain project SHALL reference another domain project.

#### Scenario: Domain isolation enforced
- **WHEN** a domain project is built
- **THEN** it references only FunkArr.Core (and transitively Messages, Persistence)

#### Scenario: No cross-domain references
- **WHEN** examining all domain project references
- **THEN** no domain project appears in another domain project's references

### Requirement: Adapter projects
`FunkArr.Api` and `FunkArr.ArrApi` SHALL each use `Microsoft.NET.Sdk` and reference only `FunkArr.Core`.

#### Scenario: Adapter references only Core
- **WHEN** an adapter project is built
- **THEN** it references only FunkArr.Core

### Requirement: Test projects
Each domain and adapter project SHALL have a corresponding test project:
`FunkArr.Search.Tests`, `FunkArr.Download.Tests`, `FunkArr.RuleSet.Tests`,
`FunkArr.MatchMagic.Tests`, `FunkArr.Api.Tests`, `FunkArr.ArrApi.Tests`.
Each test project SHALL reference its domain project and `FunkArr.Tests.Shared`.
`FunkArr.Tests.Shared` SHALL reference `FunkArr.Core`.

#### Scenario: Test project references its domain
- **WHEN** a test project is built
- **THEN** it references its corresponding domain project and Tests.Shared

### Requirement: Empty but compiling projects
All projects SHALL compile successfully with `dotnet build`. Projects that have
no source files yet SHALL contain a placeholder (empty namespace file or marker)
to satisfy the compiler.

#### Scenario: Clean build succeeds
- **WHEN** `dotnet build FunkArr.slnx` is run from `src/`
- **THEN** the build succeeds with zero errors
