## ADDED Requirements

### Requirement: Akka node liveness probe
The system SHALL expose an Akka node liveness health check at `/healthz/akka/live/node` that reports healthy when the ActorSystem is running.

#### Scenario: ActorSystem running
- **WHEN** the ActorSystem is started and running
- **THEN** a GET request to `/healthz/akka/live/node` SHALL return HTTP 200

#### Scenario: ActorSystem not started
- **WHEN** the ActorSystem has not yet started or has terminated
- **THEN** a GET request to `/healthz/akka/live/node` SHALL return HTTP 503

### Requirement: Akka node readiness probe
The system SHALL expose an Akka node readiness health check at `/healthz/akka/ready/node` that reports healthy when the ActorSystem is ready to process messages.

#### Scenario: ActorSystem ready
- **WHEN** the ActorSystem is fully initialized and ready
- **THEN** a GET request to `/healthz/akka/ready/node` SHALL return HTTP 200

### Requirement: Akka persistence liveness probe
The system SHALL expose a persistence liveness health check at `/healthz/akka/live/persistence` that periodically probes the journal and snapshot store to verify they are operational.

#### Scenario: Persistence store healthy
- **WHEN** both the journal and snapshot store are reachable and functioning
- **THEN** a GET request to `/healthz/akka/live/persistence` SHALL return HTTP 200

#### Scenario: Persistence store unreachable
- **WHEN** the journal or snapshot store is unreachable or fails a probe
- **THEN** a GET request to `/healthz/akka/live/persistence` SHALL return HTTP 503

### Requirement: Aggregate Akka health endpoints
The system SHALL expose aggregate health endpoints:
- `/healthz/akka/live` — aggregates all Akka liveness probes (node + persistence)
- `/healthz/akka/ready` — aggregates all Akka readiness probes (node)
- `/healthz/akka` — aggregates all Akka probes

#### Scenario: All probes healthy
- **WHEN** all individual Akka probes report healthy
- **THEN** a GET request to `/healthz/akka` SHALL return HTTP 200

#### Scenario: Any probe unhealthy
- **WHEN** any individual Akka probe reports unhealthy
- **THEN** a GET request to `/healthz/akka` SHALL return HTTP 503

### Requirement: Existing health infrastructure preserved
The existing `/healthz` endpoint (with FfmpegHealthCheck) and `/alive` endpoint SHALL continue to function unchanged. Akka health checks SHALL be mapped on separate routes and SHALL NOT interfere with existing health check registrations.

#### Scenario: Existing healthz still works
- **WHEN** a GET request is made to `/healthz`
- **THEN** the response SHALL include the FFmpeg health check result as before

#### Scenario: Alive endpoint unchanged
- **WHEN** a GET request is made to `/alive`
- **THEN** the response SHALL return HTTP 200 with body "Alive"

### Requirement: Health check NuGet integration
The system SHALL use `Akka.HealthCheck.Hosting.Web` package with `HealthCheckType.Default | HealthCheckType.Persistence` to register probes. Probe wiring SHALL use the three-step pattern: `WithAkkaHealthCheck` (service registration), `WithWebHealthCheck` (actor system wiring), `MapAkkaHealthCheckRoutes` (endpoint mapping).

#### Scenario: Health check registration
- **WHEN** the application starts
- **THEN** Akka health check actors SHALL be started and probes SHALL begin periodic checks
