## MODIFIED Requirements

### Requirement: Host project
`FunkArr` SHALL use `Microsoft.NET.Sdk.Web` with `OutputType: Exe`. It SHALL
reference all domain, adapter, and infrastructure projects: Core, Api, ArrApi,
Search, Download, RuleSet, MatchMagic, Messages, Persistence.

#### Scenario: Host references all projects
- **WHEN** the host project is built
- **THEN** it transitively includes all domain and adapter assemblies

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
