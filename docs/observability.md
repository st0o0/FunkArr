# Observability

FunkArr exportiert Traces und Metriken via [OpenTelemetry](https://opentelemetry.io/) (OTLP). Damit lassen sich Download-Pipelines, Scoring-Durchlaufe und Enrichment-Anfragen in jedem OTLP-kompatiblen Backend verfolgen.

## Was wird instrumentiert?

### Tracing

| Quelle | Beschreibung |
|--------|-------------|
| ASP.NET Core | Eingehende HTTP-Anfragen (alle API-Endpunkte) |
| HttpClient | Ausgehende HTTP-Anfragen (MVW, TMDB, TVDB, etc.) |
| `FunkArr.Download` | Download-Pipeline: Queue, Fetch, FFmpeg-Remux |
| `FunkArr.Scoring` | Scoring-Durchlaufe: Regelwerk-Matching, Score-Berechnung |
| `FunkArr.Enrichment` | Metadaten-Anreicherung: TMDB/TVDB-Lookups |

### Metriken

| Meter | Beschreibung |
|-------|-------------|
| ASP.NET Core | Request-Rate, Latenz, Fehlerrate |
| `FunkArr.Search` | Suchanfragen, Ergebnisse, Dauer |
| `FunkArr.Download` | Downloads, Bytes, Geschwindigkeit |
| `FunkArr.Scoring` | Scoring-Durchlaufe, Matches, Dauer |
| `FunkArr.Enrichment` | TMDB/TVDB-Lookups, Cache-Hits |

## Konfiguration

FunkArr verwendet den Standard-OTLP-Exporter. Die Konfiguration erfolgt uber die offizielle OpenTelemetry-Umgebungsvariable:

| Variable | Standard | Beschreibung |
|----------|----------|-------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | _(leer)_ | OTLP-Empfanger-URL (z.B. `http://aspire-dashboard:18889`). Ohne Wert ist der Export deaktiviert. |

::: tip
Ohne gesetzte `OTEL_EXPORTER_OTLP_ENDPOINT` startet FunkArr normal, exportiert aber keine Telemetrie-Daten. Es gibt keinen Overhead, wenn kein Empfanger konfiguriert ist.
:::

## Aspire Dashboard

Der einfachste Weg, Traces und Metriken zu betrachten, ist das [.NET Aspire Dashboard](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/standalone). Es lauft als einzelner Container neben FunkArr.

### Docker Compose Setup

```yaml
services:
  funkarr:
    image: ghcr.io/st0o0/funkarr:latest
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889
    # ... restliche FunkArr-Konfiguration

  aspire-dashboard:
    image: mcr.microsoft.com/dotnet/aspire-dashboard:latest
    ports:
      - "18888:18888"
    environment:
      - DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true
```

Nach dem Start ist das Dashboard unter `http://localhost:18888` erreichbar.

### Was du dort siehst

- **Traces** - Verteilte Traces fur jeden Request-Durchlauf: von der Suchanfrage uber Scoring und Enrichment bis zum Download
- **Metrics** - Live-Dashboards fur Request-Raten, Download-Geschwindigkeiten und Scoring-Statistiken
- **Structured Logs** - Alle Serilog-Logeintrge als strukturierte Daten (wenn ein OTLP-Log-Exporter konfiguriert ist)

## Andere Backends

Jedes OTLP-kompatible Backend funktioniert - setze `OTEL_EXPORTER_OTLP_ENDPOINT` auf den jeweiligen Receiver:

- **Grafana Tempo/Mimir** - `http://tempo:4317`
- **Jaeger** - `http://jaeger:4317`
- **Seq** - `http://seq:5341/ingest/otlp`

Weitere OpenTelemetry-Umgebungsvariablen (z.B. `OTEL_EXPORTER_OTLP_PROTOCOL`, `OTEL_SERVICE_NAME`) werden ebenfalls unterstutzt. Siehe die [OpenTelemetry SDK-Dokumentation](https://opentelemetry.io/docs/languages/net/configuration/) fur alle Optionen.
