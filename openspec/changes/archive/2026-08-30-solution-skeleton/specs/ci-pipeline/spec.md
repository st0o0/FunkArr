## ADDED Requirements

### Requirement: CI build pipeline
The CI pipeline SHALL build the entire solution via `dotnet build FunkArr.slnx`
from the `src/` directory. Build failures SHALL fail the pipeline.

#### Scenario: CI builds all projects
- **WHEN** a push or PR triggers the CI pipeline
- **THEN** all 18 projects are compiled

### Requirement: CI format check
The CI pipeline SHALL run `dotnet format --verify-no-changes` from `src/`.
Formatting violations SHALL fail the pipeline.

#### Scenario: Formatting violation fails CI
- **WHEN** a file violates .editorconfig rules
- **THEN** the CI pipeline fails with a formatting error

### Requirement: Dockerfile for multi-project solution
The Dockerfile SHALL use a multi-stage build:
1. SDK stage: restore and build the entire solution, publish the host project
2. Runtime stage: copy published output to `aspnet:chiseled-extra` base image

The Dockerfile SHALL support multi-arch builds (amd64/arm64).

#### Scenario: Docker build succeeds
- **WHEN** `docker build .` is run from the repo root
- **THEN** the image is built successfully with the FunkArr host as entrypoint

### Requirement: Dockerfile.dev for development
`Dockerfile.dev` SHALL use the SDK image with hot-reload support via `dotnet watch`.
It SHALL mount the `src/` directory for live development.

#### Scenario: Dev container starts with hot-reload
- **WHEN** the dev container is started via docker-compose
- **THEN** the FunkArr host runs with `dotnet watch` for live reload
