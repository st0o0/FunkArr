# Capability: Solution Infrastructure

## Purpose

Build tooling and shared configuration files that govern SDK version, build properties,
central package management, code style enforcement, and the solution file format.

## Requirements

### Requirement: .NET SDK pinning via global.json
The solution SHALL include a `src/global.json` that pins the .NET SDK to 10.0.102
with `rollForward: latestFeature` and `allowPrerelease: false`. The test runner
SHALL be set to `Microsoft.Testing.Platform`.

#### Scenario: SDK version enforced
- **WHEN** a developer runs `dotnet build` from `src/`
- **THEN** the build uses .NET SDK 10.0.x (latest feature band of 10.0.102)

### Requirement: Shared build properties via Directory.Build.props
The solution SHALL include `src/Directory.Build.props` that sets:
- `TargetFramework: net10.0`
- `ImplicitUsings: enable`
- `Nullable: enable`
- `Version` with `x-release-please-version` marker
- Test projects (name contains "Tests") auto-detected: `IsPackable: false`, suppress `xUnit1051`

#### Scenario: All projects inherit shared properties
- **WHEN** a new project is added to the solution
- **THEN** it inherits TargetFramework, Nullable, and ImplicitUsings without declaring them

#### Scenario: Test projects are not packable
- **WHEN** a project name contains "Tests"
- **THEN** it is marked as `IsPackable: false` and `IsTestProject: true`

### Requirement: Central package management via Directory.Packages.props
The solution SHALL use central package management with `ManagePackageVersionsCentrally: true`.
All NuGet package versions SHALL be declared only in `src/Directory.Packages.props`.
Individual csproj files SHALL NOT specify package versions.

Key package groups:
- Akka.NET: Hosting, Persistence.Sql.Hosting, Streams, Cluster.Sharding, Logger.Serilog
- Servus, Servus.Akka
- Serilog + enrichers/sinks
- xUnit v3, Akka.Hosting.TestKit, Verify.XunitV3
- ArchUnitNET
- Transitive pins for security advisories

#### Scenario: Package version declared centrally
- **WHEN** a project references a NuGet package
- **THEN** the version is resolved from Directory.Packages.props, not the csproj

### Requirement: Code style enforcement via .editorconfig
The solution SHALL include `src/.editorconfig` with default .NET style rules covering:
- Indentation (4 spaces)
- Namespace declarations (file-scoped)
- `var` usage preferences
- Naming conventions (PascalCase for public, camelCase for private, _ prefix for fields)
- Using directive ordering and placement
- Brace and newline rules

#### Scenario: Format check passes on clean solution
- **WHEN** `dotnet format --verify-no-changes` is run from `src/`
- **THEN** the command exits with code 0

### Requirement: Solution file in slnx format
The solution SHALL use the `.slnx` XML format at `src/FunkArr.slnx` containing
all projects and a solution items folder referencing `Directory.Build.props`,
`Directory.Packages.props`, and `global.json`.

#### Scenario: All projects listed in solution
- **WHEN** `dotnet sln list` is run against `FunkArr.slnx`
- **THEN** all 18 projects are listed
