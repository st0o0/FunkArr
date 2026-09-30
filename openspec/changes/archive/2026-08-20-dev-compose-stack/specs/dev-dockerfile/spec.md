## ADDED Requirements

### Requirement: Multi-stage build from source
The `Dockerfile.dev` SHALL use a multi-stage build: an SDK stage that restores and publishes the application, and a runtime stage that runs it with FFmpeg installed.

#### Scenario: Build from source without pre-publish
- **WHEN** a developer runs `docker build -f Dockerfile.dev .` from the repository root
- **THEN** the image compiles the application from source using the .NET SDK and produces a runnable container

### Requirement: NuGet restore layer caching
The build SHALL copy project files (`*.csproj`, `*.slnx`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`) before copying source code, so that NuGet restore is cached when only source files change.

#### Scenario: Source-only change rebuild
- **WHEN** a developer modifies a `.cs` file and rebuilds
- **THEN** the NuGet restore layer is reused from cache and only compilation runs

### Requirement: FFmpeg available in runtime image
The runtime stage SHALL install FFmpeg via apt so muxing operations work in the dev container.

#### Scenario: FFmpeg is callable
- **WHEN** the dev container starts
- **THEN** `ffmpeg -version` executes successfully inside the container

### Requirement: Community rulesets included
The build SHALL copy `data/community/rulesets/` into the image at `/app/data/rulesets/community/` so ruleset matching works without GitHub release download.

#### Scenario: Community rulesets present at startup
- **WHEN** the dev container starts without a mounted rulesets volume
- **THEN** community ruleset files are available at `/app/data/rulesets/community/`

### Requirement: Same ports and volumes as production
The dev image SHALL expose port 6969 and declare volumes for `/app/data` and `/media`, matching the production Dockerfile contract.

#### Scenario: Port and volume compatibility
- **WHEN** the dev image is inspected
- **THEN** it exposes port 6969 and declares `/app/data` and `/media` volumes
