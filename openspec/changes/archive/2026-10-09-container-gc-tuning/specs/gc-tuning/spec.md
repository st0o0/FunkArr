## ADDED Requirements

### Requirement: Container uses Workstation GC

The Dockerfile SHALL set `DOTNET_gcServer=0` to configure the .NET runtime for Workstation garbage collection mode instead of the Server GC default.

#### Scenario: Workstation GC is active at runtime

- **WHEN** the FunkArr container starts
- **THEN** the .NET runtime MUST use Workstation GC (single heap, concurrent collection)

#### Scenario: User can override GC mode

- **WHEN** a user sets `DOTNET_gcServer=1` in their docker-compose environment section
- **THEN** the runtime MUST use Server GC, overriding the Dockerfile default

### Requirement: Container uses maximum memory conservation

The Dockerfile SHALL set `DOTNET_GCConserveMemory=9` to configure the .NET GC for maximum memory conservation, returning unused memory to the OS as aggressively as possible.

#### Scenario: Conservative memory behavior at runtime

- **WHEN** the FunkArr container is idle after a search or download burst
- **THEN** the GC MUST return reclaimed memory to the OS promptly rather than holding it speculatively

#### Scenario: User can override conservation level

- **WHEN** a user sets `DOTNET_GCConserveMemory=0` in their docker-compose environment section
- **THEN** the runtime MUST use default (non-conservative) memory behavior, overriding the Dockerfile default

### Requirement: GC environment variables are grouped with existing runtime settings

The two new `ENV` directives SHALL be placed in the Dockerfile adjacent to the existing `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` setting, maintaining a single logical block of .NET runtime configuration.

#### Scenario: ENV variables are present in built image

- **WHEN** the Docker image is built from the Dockerfile
- **THEN** the image MUST contain environment variables `DOTNET_gcServer=0`, `DOTNET_GCConserveMemory=9`, and `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false`
