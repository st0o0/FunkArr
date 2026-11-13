# Abläufe

Wie eine Suche, ein Download, der RSS-Crawl und der Regelwerk-Sync durch die Aktoren laufen. Was jeder Schritt für Nutzer bedeutet (Timeouts, Bewertungsstrategien, FFmpeg-Optionen), steht in [So funktioniert FunkArr](/how-it-works).

## Suche

Sonarr sendet `GET /index/api?t=tvsearch&tvdbid=…&season=1&ep=5`.

<ArchitectureView view="search-flow" height="860px" />

- **Suche und Feed teilen sich die Bewertungs-Worker.** Der RSS-Crawl nutzt dieselben `Tv/MovieEvaluationWorker`; eine Änderung an Scoring oder Enrichment wirkt also auf beides.
- **Such-Worker leben nur für eine Anfrage.** Eine Entity pro Such-ID, passiviert nach der Antwort. Zustand, der eine Anfrage überdauern muss, gehört nicht dorthin.
- **Der Controller arbeitet nicht.** Welcher Worker, welches Regelwerk und welche Timeouts entscheiden Aktoren, nicht der `NewznabController`.

## Download

Sonarr übernimmt ein Release und lädt dessen NZB mit `POST /download/api?mode=addfile` hoch. Die NZB enthält die Video-URL.

<ArchitectureView view="download-flow" height="860px" />

Danach fragt Sonarr `mode=queue` und `mode=history` ab, beantwortet von `DownloadManager` und `DownloadHistoryManager`.

- **Zwei Journale.** Die Warteschlange liegt im Journal des `DownloadManager`, der Fortschritt eines Downloads im Journal seines `DownloadWorker`. Nach einem Neustart verteilt der Manager aktive Downloads neu, und jeder Worker stellt seinen eigenen Zustand wieder her.
- **Seiteneffekte nach der Persistenz.** Der Worker startet FFmpeg erst, wenn das passende Event gespeichert ist.

## RSS-Crawl und -Sync

`FeedCrawlManager` startet einen Crawl per Timer. Sein Kind `FeedHarvestActor` blättert in einer Akka.Streams-Pipeline durch neue Mediathek-Einträge und schickt sie durch dieselben Bewertungs-Worker wie eine Suche. Sonarrs RSS-Sync (eine Suche ohne Suchbegriff oder ID) wird aus dem `FeedStoreManager` beantwortet.

<ArchitectureView view="rss-sync" />

Der Regelwerk-Sync von GitHub steht auf der Seite [Regelwerke](./rulesets).
