# APIs

FunkArr serves three HTTP surfaces on port 6969, for different clients and with different rules.

| Path | Project | Client | Contract |
|---|---|---|---|
| `/api` | `FunkArr.Api` (Minimal APIs, OpenAPI) | the FunkArr web UI | owned by FunkArr, changes with the UI |
| `/index/api` | `FunkArr.ArrApi` (`NewznabController`) | Prowlarr, Sonarr, Radarr as indexer | [Newznab](https://newznab.readthedocs.io/), must stay compatible |
| `/download/api` | `FunkArr.ArrApi` (`SabnzbdController`) | Sonarr, Radarr as download client | [SABnzbd](https://sabnzbd.org/wiki/advanced/api), must stay compatible |

Plus `/metrics` for Prometheus (see [Observability](/en/observability)) and the health endpoints.

## `/api`

One endpoint class per feature (`DownloadsApiEndpoints`, `RuleSetApiEndpoints`, …) mapped under `/api/downloads`, `/api/rulesets`, `/api/mediathek`, `/api/feed`, `/api/system`, `/api/setup`. Every handler resolves an actor, asks it with a message and maps the answer to an `IResult`. Enums are sent as integers; the UI maps them to labels. The queue and the log view are pushed to the UI as server-sent events (`/api/downloads/queue/stream`, `/api/system/logs/stream`).

How such an endpoint is written: [Minimal APIs](https://anvil.st0o0.net/api/minimal-api/) and [models and mapping](https://anvil.st0o0.net/api/models-and-mapping/) on anvil.

## `/index/api`

Implements `t=caps`, `search`, `tvsearch`, `movie` and `get`. A search without query or media ID (RSS sync, the "Test" button) is answered from the RSS feed. Each release in the RSS/XML response links to an NZB whose payload is the Mediathek video URL.

## `/download/api`

Implements the SABnzbd modes Sonarr and Radarr use: `version`, `get_config`, `fullstatus`, `addfile`, `queue`, `history` and `retry`.

## Rules for the *arr adapters

- **Thin adapter.** Controllers translate protocol parameters into messages and answers back into protocol shapes. Anything that decides what to return belongs in an actor.
- **Protocol fidelity.** Sonarr and Radarr are strict about field names, types and error shapes. Errors use the protocol's own format (SABnzbd: `{ "status": false, "error": "…" }`), not problem details.
- **Unknown values.** Enums start with `Unknown = 0`, so an unexpected value binds instead of causing an automatic 400. The handler then rejects it with the protocol's own error.
