## Context

FunkArr runs as a Docker container. Logs go to stdout and are scraped by Grafana Alloy/Promtail into Loki. The current plain-text console output requires fragile regex parsing and loses structured properties that Serilog captures internally. The `HttpPort` option is infrastructure config that doesn't belong in `FunkArrOptions`.

## Goals / Non-Goals

**Goals:**
- JSON (CLEF) log output for production, toggleable via `FunkArr__LogFormat`
- Remove `HttpPort` from domain options, hardcode internal port to `6969`
- Enrich logs with application version for deployment traceability

**Non-Goals:**
- File-based log sinks (Docker stdout is the standard)
- Log aggregation configuration (Alloy/Loki pipeline is ops concern)
- Changing log levels or filtering (stays in Serilog config sections)

## Decisions

### Decision 1: CLEF via Serilog.Formatting.Compact

Use `CompactJsonFormatter` from `Serilog.Formatting.Compact`. CLEF is the standard format that Alloy's `loki.source.docker` + `json` stage parses natively. Properties use `@t` (timestamp), `@l` (level), `@mt` (message template), `@x` (exception) — all directly queryable in Loki.

**Alternative**: Serilog's built-in `JsonFormatter` — more verbose, less tooling support, no standard property names.

### Decision 2: LogFormat option in FunkArrOptions

Add `LogFormat` as a string property on `FunkArrOptions` (values: `json`, `text`, default `text`). Validated in `FunkArrOptionsValidator`. The Serilog configuration in `Program.cs` reads this value early (before full DI) via `IConfiguration` to decide which formatter to use on the console sink.

**Alternative**: Separate Serilog config section per environment — harder to document, more error-prone, no single env var toggle.

### Decision 3: Hardcoded internal port 6969

Set `ASPNETCORE_URLS` to `http://+:6969` in `Program.cs` as a fallback (only when `ASPNETCORE_URLS` is not already set). Remove `HttpPort` from `FunkArrOptions` and its validator. Update `Dockerfile` to `EXPOSE 6969` and `docker-compose.example.yml` to map `8080:6969`.

### Decision 4: Application version enrichment

Use `Assembly.GetEntryAssembly().GetName().Version` to enrich all log events with an `ApplicationVersion` property. This is set once at startup via `.Enrich.WithProperty()`.

## Risks / Trade-offs

- **[Breaking change: HttpPort removal]** → Documented in proposal, migration path is Docker port mapping or `ASPNETCORE_URLS`. Low risk since this is a new project with no existing users.
- **[LogFormat default is text, not json]** → Developers get readable output by default. Docker users must set `FunkArr__LogFormat=json`. Mitigated by documenting it in `docker-compose.example.yml`.
- **[Early config read for LogFormat]** → Serilog is configured before DI, so we read `LogFormat` from `IConfiguration` directly, not from `IOptions<FunkArrOptions>`. This means validation happens after Serilog is already configured. Acceptable because an invalid value still fails startup via `ValidateOnStart`.
