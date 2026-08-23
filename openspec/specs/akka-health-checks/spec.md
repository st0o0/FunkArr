## Purpose

Akka.NET actor system and persistence health probes via built-in `Akka.Hosting` health check APIs, exposing liveness and readiness checks through the standard ASP.NET Core health check infrastructure.

## Requirements

### Requirement: Akka ActorSystem liveness check
The system SHALL register an ActorSystem liveness health check via `WithActorSystemLivenessCheck()` that reports healthy when the ActorSystem is running. The check SHALL be tagged with `akka` and integrate with the existing `/healthz` endpoint.

#### Scenario: ActorSystem running
- **WHEN** the ActorSystem is started and running
- **THEN** the health check SHALL report Healthy

#### Scenario: ActorSystem terminated
- **WHEN** the ActorSystem has terminated
- **THEN** the health check SHALL report Unhealthy

### Requirement: Persistence journal health check
The system SHALL register a journal health check via `WithSqlPersistence` journal builder's `.WithHealthCheck()` that verifies the persistence journal is accessible and operational.

#### Scenario: Journal healthy
- **WHEN** the journal storage is reachable and functioning
- **THEN** the health check SHALL report Healthy

#### Scenario: Journal unreachable
- **WHEN** the journal storage is unreachable or fails a probe
- **THEN** the health check SHALL report Unhealthy

### Requirement: Persistence snapshot store health check
The system SHALL register a snapshot store health check via `WithSqlPersistence` snapshot builder's `.WithHealthCheck()` that verifies the snapshot store is accessible and operational.

#### Scenario: Snapshot store healthy
- **WHEN** the snapshot store is reachable and functioning
- **THEN** the health check SHALL report Healthy

#### Scenario: Snapshot store unreachable
- **WHEN** the snapshot store is unreachable or fails a probe
- **THEN** the health check SHALL report Unhealthy

### Requirement: Existing health infrastructure preserved
The existing `/healthz` endpoint (with FfmpegHealthCheck) and `/alive` endpoint SHALL continue to function unchanged. Akka health checks SHALL integrate with the existing `Microsoft.Extensions.Diagnostics.HealthChecks` infrastructure and appear in `/healthz` alongside existing checks.

#### Scenario: Existing healthz includes Akka checks
- **WHEN** a GET request is made to `/healthz`
- **THEN** the response SHALL include both the FFmpeg health check result and all Akka health check results

#### Scenario: Alive endpoint unchanged
- **WHEN** a GET request is made to `/alive`
- **THEN** the response SHALL return HTTP 200 with body "Alive"

### Requirement: Built-in Akka.Hosting health check API
The system SHALL use the health check APIs built into `Akka.Hosting` 1.5.70+ (not the deprecated `Akka.HealthCheck.Hosting.Web` package). Health checks SHALL be wired via `WithActorSystemLivenessCheck()` on the `AkkaConfigurationBuilder` and `.WithHealthCheck()` on journal/snapshot builders within `WithSqlPersistence()`.

#### Scenario: Health check registration
- **WHEN** the application starts
- **THEN** Akka health checks SHALL be registered with `Microsoft.Extensions.Diagnostics.HealthChecks` and begin periodic checks
