## Purpose

Environment-driven log format switching (JSON CLEF for production, plain text for development) with structured enrichment compatible with Grafana Alloy/Promtail/Loki pipeline.

## Requirements

### Requirement: Environment-driven log format selection
The application SHALL support a `LogFormat` configuration option (under the `FunkArr` section) that accepts `json` or `text` values, defaulting to `text`.

#### Scenario: JSON log output in production
- **WHEN** the application starts with `FunkArr__LogFormat=json`
- **THEN** all console log output SHALL use Compact Log Event Format (CLEF) JSON, with each log event as a single JSON line

#### Scenario: Plain text log output for development
- **WHEN** the application starts with `FunkArr__LogFormat=text` or the option is not set
- **THEN** console log output SHALL use the human-readable template `[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}`

#### Scenario: Invalid log format value
- **WHEN** the application starts with `FunkArr__LogFormat=xml` or any value other than `json` or `text`
- **THEN** the application SHALL fail validation on startup with a descriptive error message

### Requirement: Structured log enrichment
Log events SHALL be enriched with machine name, thread ID, log context, and application version.

#### Scenario: JSON log entry contains enrichment properties
- **WHEN** a log event is written in JSON format
- **THEN** the JSON object SHALL contain `MachineName`, `ThreadId`, `SourceContext`, and `ApplicationVersion` properties

### Requirement: Multi-line exception atomicity in JSON mode
In JSON mode, exceptions SHALL be serialized as part of the JSON object, not as separate log lines.

#### Scenario: Exception logged in JSON mode
- **WHEN** an exception with a multi-line stack trace is logged with `FunkArr__LogFormat=json`
- **THEN** the entire exception including stack trace SHALL be contained within a single JSON line in the `@x` property
