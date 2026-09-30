## 1. New observability page (DE + EN)

- [x] 1.1 Create `docs/observability.md` (DE) covering OpenTelemetry tracing (custom sources: FunkArr.Download, FunkArr.Scoring, FunkArr.Enrichment), metrics (custom meters: FunkArr.Search, FunkArr.Download, FunkArr.Scoring, FunkArr.Enrichment), OTLP endpoint config (`OTEL_EXPORTER_OTLP_ENDPOINT`), and Aspire Dashboard docker-compose setup (port 18888 UI, port 18889 OTLP receiver)
- [x] 1.2 Create `docs/en/observability.md` (EN) with same content
- [x] 1.3 Add observability page to sidebar and nav in `docs/.vitepress/config.ts` for both DE and EN locales

## 2. Extend web-ui.md (DE + EN)

- [x] 2.1 Add Download Detail section to `docs/web-ui.md`: clickable queue cards navigate to `/activity/:id`, shows title/status/channel badges, progress bar with speed/ETA, details grid (category, size, phase, priority, file path, duration), failure message, actions (Force Start, Retry, Delete)
- [x] 2.2 Add Settings page section to `docs/web-ui.md`: five sections (Downloads with concurrent/schedule, Metadata Cache stats, Network Routes, System info with versions, Logs with real-time SSE log viewer filterable by level)
- [x] 2.3 Apply same additions to `docs/en/web-ui.md`

## 3. Expand landing page (DE + EN)

- [x] 3.1 Update `docs/index.md` feature tiles from 4 to 6: add Proxy/Geo-Routing tile and Observability tile
- [x] 3.2 Update `docs/en/index.md` with same expanded tiles

## 4. Fix README

- [x] 4.1 Fix config table: rename `FunkArr__MatchHistory__` prefix to `FunkArr__ScoringHistory__`
- [x] 4.2 Fix Build & Test section: replace `FunkArr.MatchMagic.Tests` with `FunkArr.Scoring.Tests`, `FunkArr.MetadataResolver.Tests` with `FunkArr.Enrichment.Tests`, add `FunkArr.History.Tests`
- [x] 4.3 Expand features list: add health checks, network routing/proxy, download scheduling, Settings page with log viewer, OpenTelemetry/observability, PUID/PGID support
- [x] 4.4 Add missing config vars to README table: `PUID`/`PGID`, `OTEL_EXPORTER_OTLP_ENDPOINT`, download schedule, and link to docs site for full reference
