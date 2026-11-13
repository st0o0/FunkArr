# APIs

FunkArr bedient auf Port 6969 drei HTTP-Schnittstellen, für verschiedene Clients und mit verschiedenen Regeln.

| Pfad | Projekt | Client | Vertrag |
|---|---|---|---|
| `/api` | `FunkArr.Api` (Minimal APIs, OpenAPI) | die FunkArr-Web-Oberfläche | gehört FunkArr, ändert sich mit der Oberfläche |
| `/index/api` | `FunkArr.ArrApi` (`NewznabController`) | Prowlarr, Sonarr, Radarr als Indexer | [Newznab](https://newznab.readthedocs.io/), muss kompatibel bleiben |
| `/download/api` | `FunkArr.ArrApi` (`SabnzbdController`) | Sonarr, Radarr als Downloadclient | [SABnzbd](https://sabnzbd.org/wiki/advanced/api), muss kompatibel bleiben |

Dazu kommen `/metrics` für Prometheus (siehe [Observability](/observability)) und die Health-Endpunkte.

## `/api`

Eine Endpunkt-Klasse pro Bereich (`DownloadsApiEndpoints`, `RuleSetApiEndpoints`, …), gemappt unter `/api/downloads`, `/api/rulesets`, `/api/mediathek`, `/api/feed`, `/api/system`, `/api/setup`. Jeder Handler löst einen Aktor auf, fragt ihn mit einer Nachricht und macht aus der Antwort ein `IResult`. Enums werden als Zahlen übertragen; die Oberfläche macht Beschriftungen daraus. Warteschlange und Log-Ansicht werden als Server-Sent Events an die Oberfläche geschickt (`/api/downloads/queue/stream`, `/api/system/logs/stream`).

Wie so ein Endpunkt geschrieben wird: [Minimal APIs](https://anvil.st0o0.net/api/minimal-api/) und [Modelle und Mapping](https://anvil.st0o0.net/api/models-and-mapping/) auf anvil.

## `/index/api`

Unterstützt `t=caps`, `search`, `tvsearch`, `movie` und `get`. Eine Suche ohne Suchbegriff oder Medien-ID (RSS-Sync, der „Test“-Knopf) wird aus dem RSS-Feed beantwortet. Jedes Release in der RSS/XML-Antwort verweist auf eine NZB, deren Inhalt die Mediathek-Video-URL ist.

## `/download/api`

Unterstützt die SABnzbd-Modi, die Sonarr und Radarr nutzen: `version`, `get_config`, `fullstatus`, `addfile`, `queue`, `history` und `retry`.

## Regeln für die *arr-Adapter

- **Dünner Adapter.** Controller übersetzen Protokollparameter in Nachrichten und Antworten zurück ins Protokollformat. Alles, was entscheidet, was zurückkommt, gehört in einen Aktor.
- **Protokolltreue.** Sonarr und Radarr sind streng bei Feldnamen, Typen und Fehlerformaten. Fehler nutzen das Format des Protokolls (SABnzbd: `{ "status": false, "error": "…" }`), keine Problem Details.
- **Unbekannte Werte.** Enums beginnen mit `Unknown = 0`, ein unerwarteter Wert wird also gebunden statt automatisch mit 400 abgelehnt. Der Handler weist ihn dann mit dem Fehlerformat des Protokolls ab.
