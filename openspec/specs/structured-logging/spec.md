## Purpose

Structured logging via Serilog with plain-text console output and enrichment properties for observability.

## Requirements

### Requirement: Serilog console logging with fixed template
The application SHALL use Serilog with a console sink and a single plain-text output template. There is no configurable log format option.

#### Scenario: Plain text log output
- **WHEN** the application starts
- **THEN** console log output SHALL use the human-readable template `[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}`

### Requirement: Structured log enrichment
Log events SHALL be enriched with machine name, thread ID, log context, and application version.

#### Scenario: Log entries contain enrichment properties
- **WHEN** a log event is written
- **THEN** the log context SHALL include `MachineName`, `ThreadId`, `SourceContext`, and `ApplicationVersion` enrichment properties via Serilog's `Enrich.WithMachineName()`, `Enrich.WithThreadId()`, `Enrich.FromLogContext()`, and `Enrich.WithProperty("ApplicationVersion", ...)` enrichers

### Requirement: Akka.NET Serilog integration
The application SHALL use Akka.Logger.Serilog so that all Akka.NET internal logging flows through the Serilog pipeline.

#### Scenario: Akka log events appear in Serilog output
- **WHEN** the Akka actor system logs a message (e.g. actor lifecycle, supervision)
- **THEN** the message SHALL appear in the Serilog console output with the same template and enrichment as application-level log entries
