# So funktioniert FunkArr

Diese Seite erklärt, wie FunkArr intern arbeitet: vom Eingang einer Suchanfrage über das Scoring und die Metadaten-Anreicherung bis zum fertigen Download.

## Architektur-Überblick

FunkArr basiert auf Akka.NET. Die Arbeit verteilt sich auf wenige langlebige Singleton-Aktoren (Manager) und kurzlebige, geshardete Entitäten (Worker). Die Domänen kommunizieren ausschließlich über Nachrichten.

| Domäne | Aktoren | Aufgabe |
|--------|---------|---------|
| Search | `SearchManager`, `MediathekViewWebManager` (Singletons), `TvSearchWorker`, `MovieSearchWorker` (geshardet, einer pro Suchanfrage) | Koordiniert eine Suche und fragt MediathekViewWeb ab |
| RuleSet | `RuleSetResolver`, `RuleSetManager`, `RuleSetUpdater` (Singletons), `RuleSetWorker` (geshardet, einer pro Regelwerk) | Lädt, vereint und löst Regelwerke auf, synchronisiert Community-Regelwerke |
| Scoring | `ScoringManager` (Singleton) mit einem Pool aus `ScoringActor` | Wertet Mediathek-Einträge gegen Regelwerk-Regeln aus |
| Enrichment | `EnrichmentManager` (Singleton) mit TVDB- und TMDB-Aktor-Pools | Bestimmt Staffel/Episode und Filmdaten über externe APIs |
| History | `StatsCollector` (Singleton), `HistoryWorker` (geshardet, persistent, einer pro Regelwerk) | Scoring-Verlauf und Statistiken |
| Download | `DownloadManager`, `DownloadScheduler`, `DownloadHistoryManager` (Singletons), `DownloadWorker` (geshardet, persistent, einer pro Download) | Warteschlange, Zeitplan, FFmpeg-Remux, Download-Verlauf |

Die HTTP-Schicht (`/index/api` für Newznab, `/download/api` für SABnzbd) ist ein dünner Adapter, der Anfragen nur in Nachrichten an diese Aktoren übersetzt.

## Ablauf einer Suche

Wenn Sonarr oder Radarr eine Suchanfrage an FunkArr senden, durchläuft diese mehrere Stufen:

### 1. Newznab-API empfängt die Anfrage

Sonarr/Radarr senden eine Standard-Newznab-Anfrage an `/index/api`. FunkArr erkennt den Typ anhand des Parameters:

- `t=tvsearch` - Seriensuche (von Sonarr), mit optionaler TVDB-ID, Staffel und Episode
- `t=movie` - Filmsuche (von Radarr), mit optionaler IMDB-ID oder TMDB-ID
- `t=search` - Allgemeine Suche (von Prowlarr), Kategorie bestimmt den Typ
- `t=caps` - Fähigkeiten des Indexers
- `t=get` - Lädt die NZB zu einem Suchergebnis

Die Anfrage wird gegen den API-Key geprüft, in einen internen Suchbefehl umgewandelt und an den SearchManager weitergeleitet. Die vollständige Ergebnisliste einer Suche wird 60 Sekunden zwischengespeichert (`SearchCacheTtlSeconds`), sodass Pagination und der Staffel-/Episoden-Filter auf der gecachten Liste arbeiten, ohne die Suche erneut auszuführen.

### 2. SearchManager koordiniert die Suche

Der SearchManager (Cluster-Singleton) erzeugt eine Such-ID und übergibt die Arbeit an einen geshardeten Such-Worker (je eine `TvSearchWorker`- oder `MovieSearchWorker`-Entität pro Suchanfrage). Explizite `t=tvsearch`- / `t=movie`-Anfragen gehen direkt an den passenden Worker. Bei `t=search` entscheidet die Kategorie:

- Kategorie 5000-5999: nur Serien
- Kategorie 2000-2999: nur Filme
- Keine Kategorie: beides parallel, Ergebnisse werden zusammengeführt

Jede Suche hat ein Timeout von 30 Sekunden (der Controller selbst bricht nach 45 Sekunden ab). Wenn bei einer kombinierten Suche ein Teil fehlschlägt oder das Timeout erreicht, werden die Ergebnisse des anderen Teils trotzdem zurückgegeben.

Der Such-Worker durchläuft die folgenden Schritte als kleine Zustandsmaschine (Regelwerk auflösen, abfragen, bewerten, anreichern) und antwortet dann dem SearchManager. Inaktive Worker werden nach 30 Sekunden passiviert.

### 3. MediathekViewWeb-Abfrage

:::tip MediathekViewWeb
[MediathekViewWeb](https://mediathekviewweb.de/#everywhere=true) ist ein freies Community-Projekt, das die Mediatheken der deutschsprachigen öffentlich-rechtlichen Sender durchsuchbar macht. FunkArr nutzt deren API als Datenquelle für alle Suchanfragen.
:::

Der MediathekViewWebManager (Cluster-Singleton) sendet die Suchanfrage an die MediathekViewWeb-API. Diese durchsucht die Mediathek-Datenbank (ARD, ZDF, ORF, SRF und weitere Sender).

Die Abfragen filtern nach Topic (Sendungsname), sind nach Datum absteigend sortiert, schließen zukünftige Einträge aus und ignorieren Einträge unter 5 Minuten Länge. API-Antworten werden 5 Minuten zwischengespeichert. Die Ergebnisse enthalten für jeden Eintrag:

- Sender, Thema, Titel, Beschreibung
- Video-URLs in verschiedenen Qualitäten (HD, normal, niedrig)
- Untertitel-URL (falls verfügbar)
- Dauer, Größe, Ausstrahlungszeitpunkt

FunkArr begrenzt die gleichzeitigen Abfragen an MediathekViewWeb auf 3, um die API nicht zu überlasten. Weitere Anfragen warten in einer begrenzten Warteschlange (64 Einträge). Ist diese voll, versucht der Such-Worker es zweimal erneut (nach 0,5 s und 1,5 s), bevor die Suche fehlschlägt.

### 4. Regelwerk-Zuordnung (RuleSet Matching)

Der `RuleSetResolver` (Singleton) kennt alle geladenen Regelwerke und findet das passende anhand von Topic oder Alias bzw. anhand von TVDB-, IMDB- oder TMDB-ID. Das Regelwerk bestimmt, wie Mediathek-Titel in strukturierte Staffel-/Episodenformate umgewandelt werden.

Die Reihenfolge hängt von der Anfrage ab:

- **Mit TVDB-/IMDB-ID:** Zuerst wird das Regelwerk aufgelöst. Dessen Topic wird dann für die Mediathek-Abfrage verwendet.
- **Ohne ID:** Zuerst läuft die Mediathek-Abfrage mit dem Suchbegriff als Topic. Anschließend wird das Regelwerk anhand des Topics des ersten Ergebnisses aufgelöst.

Wird kein Regelwerk gefunden, werden die Mediathek-Ergebnisse ohne Scoring zurückgegeben.

Regelwerke werden getrennt von der Suche verwaltet:

- Der `RuleSetManager` scannt die Verzeichnisse für Community- und lokale Regelwerke, überwacht sie auf Änderungen (Änderungen werden 2 Sekunden gebündelt) und lädt jedes Regelwerk in einen geshardeten `RuleSetWorker`.
- Der `RuleSetWorker` vereint die Community- und die lokale Version eines Regelwerks, validiert es, registriert es beim `RuleSetResolver` und sendet seine Matching-Konfiguration an den `ScoringManager`.
- Der `RuleSetUpdater` prüft alle 30 Minuten die GitHub-Releases des Community-Regelwerk-Repositories (wenn `RefreshEnabled`), lädt die `rulesets.zip` eines neuen Releases herunter und löst einen Re-Scan aus.

### 5. Scoring

Die Scoring-Engine wertet jeden Mediathek-Eintrag gegen die Regeln des Regelwerks aus. Dabei werden drei Schritte für jede Regel durchlaufen (Regeln sind nach Priorität sortiert, die erste passende gewinnt):

**Filter-Prüfung:** Jede Regel kann Filter definieren, die ein Eintrag erfüllen muss. Filter können auf verschiedene Felder prüfen (Titel, Thema, Sender, Dauer, Beschreibung) mit Operatoren wie Gleichheit, Enthält, Regex, größer/kleiner. Filter lassen sich mit `all` (alle müssen passen), `any` (einer muss passen) und `not` (keiner darf passen) kombinieren.

**Identifikation:** Wenn ein Eintrag die Filter besteht, versucht die Regel Staffel und Episode zu erkennen. Es gibt fünf Strategien:

- `SeasonAndEpisodeNumber` - extrahiert Staffel und Episode per Regex aus dem Titel
- `AbsoluteEpisodeNumber` - extrahiert eine absolute Episodennummer per Regex
- `TitleExact` - rekonstruiert den erwarteten Titel aus Teilen und prüft auf exakte Übereinstimmung
- `TitleIncludes` - prüft ob ein konstruierter Titel im Mediathek-Titel enthalten ist (mit Umlaut-Normalisierung)
- `AirdateExtraction` - extrahiert ein deutsches Datum aus dem Titel (z.B. "15.03.2026" oder "15. März 2026")

**Bewertung:** Jeder Treffer bekommt einen Konfidenzwert (0.0-1.0). Dieser Wert stammt aus der Regel selbst oder dem Standard-Konfidenzwert des Regelwerks.

Der `ScoringManager` hält die Matching-Konfiguration jedes Regelwerks und verteilt die Arbeit auf einen Pool aus `ScoringActor`-Workern (Smallest-Mailbox-Routing). Die Poolgröße ist konfigurierbar über `FunkArr__Scoring__PoolSize` (Standard: 4). Die Regex-Auswertung hat ein Timeout von 100 ms pro Treffer als Schutz vor ausufernden Mustern. Existiert keine Matching-Konfiguration für ein Regelwerk, werden die Einträge unbewertet zurückgegeben.

### 6. Metadaten-Anreicherung

Nach dem Scoring werden die Treffer mit externen Metadaten angereichert (siehe Abschnitt [Metadaten-Anreicherung](#metadaten-anreicherung)). Dieser Schritt ist optional: Ist die Anreicherung für das Regelwerk nicht konfiguriert oder schlägt sie fehl, werden die bewerteten Ergebnisse unverändert zurückgegeben. Anschließend protokolliert der Such-Worker den Lauf im Scoring-Verlauf des Regelwerks (siehe [Scoring-Verlauf](#scoring-verlauf)).

### 7. Newznab-Antwort

Die Ergebnisse werden als Newznab-RSS-Feed zurückgegeben. Jeder Eintrag enthält:

- Titel im Format "Sendungsname S01E05 Episodentitel 1080p"
- Geschätzte Dateigröße
- Kategorie (TV oder Film, mit Qualitätsstufe)
- Metadaten-Attribute (TVDB-ID, IMDB-ID, TMDB-ID, Staffel, Episode)
- NZB-Download-Link (enthält die Video-URL als Payload)

Suchergebnisse werden zwischengespeichert, damit eine Folgeanfrage mit Pagination nicht erneut die gesamte Suche auslöst.

## Download-Pipeline

Wenn Sonarr oder Radarr einen Download starten, senden sie eine SABnzbd-Anfrage an `/download/api`. FunkArr simuliert einen SABnzbd-Download-Client.

### Warteschlange

Der `DownloadManager` (persistenter Cluster-Singleton) verwaltet die Warteschlange und verteilt Downloads an geshardete, persistente `DownloadWorker`-Entitäten (eine pro Download). Wenn ein Worker fertig ist oder endgültig fehlschlägt, gibt er seinen Slot frei, trägt das Ergebnis im `DownloadHistoryManager` ein und der Manager startet den nächsten Eintrag. Die Warteschlange unterstützt:

- **Parallele Downloads:** Standardmäßig werden bis zu 3 Downloads gleichzeitig ausgeführt (konfigurierbar über `FunkArr__Download__ConcurrentDownloads`).
- **Prioritäten:** Jeder Download hat eine Priorität. Downloads mit höherer Priorität werden bevorzugt abgearbeitet.
- **Pausieren/Fortsetzen:** Die gesamte Warteschlange kann pausiert und wieder fortgesetzt werden.
- **Force-Start:** Einzelne Downloads können sofort gestartet werden, auch wenn die maximale Anzahl paralleler Downloads bereits erreicht ist.
- **Umordnen:** Downloads können an eine bestimmte Position in der Warteschlange verschoben oder miteinander getauscht werden.
- **Löschen:** Wartende oder aktive Downloads können aus der Warteschlange entfernt werden.
- **Wiederholen:** Fehlgeschlagene Downloads können erneut in die Warteschlange eingereiht werden.

Die Warteschlange ist persistent - sie überlebt Neustart des Containers. Aktive Downloads werden nach einem Neustart automatisch erneut gestartet.

### Download-Zeitplan

FunkArr unterstützt optionale Download-Zeitfenster. Der `DownloadScheduler` (Singleton) wertet die konfigurierten Fenster aus und teilt dem DownloadManager mit, ob Downloads aktiv oder gesperrt sind. Außerhalb der Fenster bleiben Downloads in der Warteschlange. Neue Downloads werden automatisch gestartet, sobald das nächste Zeitfenster beginnt. Fenster dürfen über Mitternacht gehen, Konfigurationsänderungen werden ohne Neustart übernommen.

### FFmpeg-Remux

Jeder Download wird von einem eigenen DownloadWorker verarbeitet, dessen Zustand (initialisiert, lädt, abgeschlossen, fehlgeschlagen) persistiert wird:

1. **Untertitel vorbereiten:** Wenn eine Untertitel-URL vorhanden ist, wird die Untertiteldatei heruntergeladen. FunkArr erkennt das Format automatisch:
   - **TTML/XML** - wird nach SRT konvertiert (über einen eigenen TTML-zu-SRT-Konverter)
   - **WebVTT** - wird direkt als VTT-Datei verwendet
   - **SRT** - wird direkt verwendet

2. **Video herunterladen und remuxen:** FFmpeg lädt das Video herunter (direkte URL oder HLS-Stream) und verpackt es als MKV-Container. Wenn für den Sender eine Netzwerk-Route mit Proxy konfiguriert ist, werden sowohl der Untertitel-Download als auch FFmpeg über den konfigurierten HTTP-Proxy geroutet (siehe [Konfiguration - Netzwerk-Routen](/configuration#netzwerk-routen)):
   - Video- und Audio-Codecs werden kopiert (keine Neucodierung)
   - Untertitel werden als SRT-Spur mit deutschem Sprach-Tag (`language=deu`) eingebettet
   - Der Fortschritt wird live ausgelesen (heruntergeladene Bytes, Zeitposition, Geschwindigkeit)

3. **Fertigstellung:** Die fertige Datei wird vom `incomplete/`-Verzeichnis in das `complete/`-Verzeichnis verschoben, organisiert nach Kategorie-Unterverzeichnis (z.B. `complete/tv/`).

4. **Wiederholung bei Fehlern:** Sind Wiederholungen aktiviert (`RetryEnabled`, standardmäßig aus), werden vorübergehende Fehler bis zu `MaxRetries`-mal wiederholt (Standard: 3). Die Wartezeit verdoppelt sich mit jedem Versuch, beginnt bei 30 Sekunden (`RetryBackoffBase`) und ist auf 5 Minuten begrenzt. Dauerhafte Fehler und erschöpfte Wiederholungen landen als fehlgeschlagen im Download-Verlauf.

### Download-Verlauf

Jeder abgeschlossene Download (erfolgreich oder fehlgeschlagen) wird vom `DownloadHistoryManager` (persistenter Singleton) aufgezeichnet. Es werden höchstens 1000 Einträge behalten (`MaxHistoryRecords`), die ältesten werden entfernt. Der Verlauf speichert Titel, Kategorie, Dateigröße, Status, Fehlermeldung (bei Fehler), Download-Dauer und Abschlusszeitpunkt. Du kannst Verlaufseinträge einzeln entfernen.

### Live-Fortschritt

Die Download-Warteschlange kann über die API abgefragt werden. Für jeden aktiven Download werden angezeigt:

- Aktuell heruntergeladene Bytes
- Aktuelle Zeitposition im Video
- Download-Geschwindigkeit
- Gesamtdauer und -größe

## Scoring-System

Das Scoring-System (auch "Match Intelligence" genannt) besteht aus zwei Komponenten: der Scoring-Engine und dem Scoring-Verlauf.

### Scoring-Engine

Die Scoring-Engine wertet Mediathek-Einträge gegen Regelwerk-Regeln aus. Der Ablauf ist im Abschnitt [Scoring](#5-scoring) oben beschrieben.

Wichtig: Jede Auswertung erzeugt einen vollständigen Trace. Der Trace dokumentiert für jeden Eintrag und jede Regel:

- Welche Filter geprüft wurden und ob sie bestanden haben
- Welche Felder mit welchen Werten verglichen wurden
- Welche Identifikationsstrategie verwendet wurde
- Warum eine Identifikation fehlgeschlagen ist (z.B. "Regex hat nicht gematcht")
- Welche Regel am Ende gematcht hat und mit welchem Konfidenzwert

Diese Traces sind in der Web-UI einsehbar und helfen beim Debuggen von Regelwerken.

### Scoring-Verlauf

Jede Suchanfrage, die über ein Regelwerk ausgewertet wird, wird im Scoring-Verlauf des jeweiligen Regelwerks gespeichert. Der Such-Worker sendet das Ergebnis an einen geshardeten, persistenten `HistoryWorker` (einer pro Regelwerk), der die Snapshots speichert und nach Anzahl und Alter beschneidet. Nach jeder Aufzeichnung meldet er aktualisierte Statistiken an den `StatsCollector` (Singleton), der die Statistiken aller Regelwerke für die UI im Speicher hält und sie beim Start aus den History-Workern wiederherstellt. Pro Snapshot werden aufgezeichnet:

- Quelle der Suche (Sonarr, Radarr, Prowlarr oder Test)
- Suchbegriff
- Zeitstempel
- Anzahl der Kandidaten, Treffer und angereicherten Ergebnisse
- Vollständige Item-Traces

Aus den Snapshots berechnet FunkArr Statistiken pro Regelwerk:

- **Match-Rate:** Anteil der Mediathek-Einträge, die von einer Regel erkannt wurden
- **Enrichment-Rate:** Anteil der erkannten Einträge, die erfolgreich mit Metadaten angereichert wurden
- **Letzter Lauf:** Zeitpunkt der letzten Auswertung

Die Konfigurationsoptionen für den Verlauf:

| Variable | Standard | Beschreibung |
|----------|----------|-------------|
| `FunkArr__ScoringHistory__MaxSnapshots` | `100` | Maximale Anzahl gespeicherter Snapshots pro Regelwerk |
| `FunkArr__ScoringHistory__MaxAgeDays` | `30` | Snapshots älter als diese Anzahl Tage werden gelöscht |
| `FunkArr__ScoringHistory__SnapshotInterval` | `20` | Intervall zwischen Snapshot-Persistierungen |

## Metadaten-Anreicherung

FunkArr kann Suchergebnisse mit externen Metadaten anreichern, um Staffel- und Episodennummern zu bestimmen, wenn das Regelwerk allein nicht ausreicht.

### TVDB (Serien)

Der `EnrichmentManager` (Singleton) leitet Anfragen an zwei Aktor-Pools (je zwei Worker) weiter: einen für TVDB, einen für TMDB. Für Serien lädt FunkArr die Episodenliste von TVDB und versucht, Mediathek-Einträge den richtigen Episoden zuzuordnen. Wurde in der Suche eine Staffel angegeben, wird zuerst nur innerhalb dieser Staffel gematcht; Einträge ohne Treffer werden anschließend gegen alle Episoden erneut versucht, mit 10% Konfidenz-Abschlag. Dabei werden zwei Methoden nacheinander versucht:

**Titel-Matching:** Vergleicht den Mediathek-Titel mit den TVDB-Episodennamen über Levenshtein-Distanz (Ähnlichkeitsmessung). Der Standard-Schwellwert liegt bei 0.7 (70% Ähnlichkeit). Bei Gleichstand zwischen mehreren Episoden wird die Laufzeit als Tiebreaker verwendet.

**Airdate-Matching:** Wenn der Mediathek-Eintrag ein Ausstrahlungsdatum hat, wird die TVDB-Episode mit dem nächsten passenden Datum gesucht. Standard-Toleranz: 7 Tage. Dieses Matching wird nur durchgeführt, wenn der beste Titel-Score mindestens 0.3 beträgt (Sicherheitsnetz gegen völlig falsche Zuordnungen).

Die TVDB-Episodenlisten werden zwischengespeichert. Serien mit zukünftigen Episoden werden 2 Tage gecacht, abgeschlossene Serien 7 Tage.

Konfiguration: `FunkArr__Tvdb__ApiKey`

### TMDB (Filme)

Für Filme fragt FunkArr TMDB ab, um Filmdaten aufzulösen (Titel, Erscheinungsjahr, IMDB-ID). Dabei werden auch alternative Titel berücksichtigt. Die Zuordnung verwendet:

- **Titel-Ähnlichkeit** über Levenshtein-Distanz (Schwellwert: 0.5)
- **Jahres-Validierung** mit einer Standard-Toleranz von 1 Jahr

TMDB-Filmdaten werden 30 Tage zwischengespeichert.

Konfiguration: `FunkArr__Tmdb__ApiKey`

### Ohne API-Schlüssel

Wenn keine API-Schlüssel konfiguriert sind, verlässt sich FunkArr ausschließlich auf Regelwerk-Muster für die Zuordnung. Das funktioniert, liefert aber weniger und weniger genaue Ergebnisse.

## Videoqualität

Mediathek-Einträge bieten Videos in bis zu drei Qualitätsstufen an. FunkArr erstellt für jede verfügbare Stufe einen eigenen Suchergebnis-Eintrag:

| Qualität | Quelle | Geschätzte Bitrate | Beispiel 60 min |
|-----------|--------|---------------------|-----------------|
| 1080p (HD) | `url_video_hd` | ~6.5 Mbit/s | ~490 MB |
| 720p (Normal) | `url_video` | ~3.3 Mbit/s | ~250 MB |
| 480p (Niedrig) | `url_video_low` | ~0.8 Mbit/s | ~60 MB |

Nicht jeder Mediathek-Eintrag hat alle drei Qualitäten. Wenn nur eine normale URL vorhanden ist, wird nur ein 720p-Ergebnis erstellt.

Die geschätzte Dateigröße wird aus der Videodauer und der typischen Bitrate pro Qualitätsstufe berechnet. Wenn der Mediathek-Eintrag eine tatsächliche Größe meldet, wird diese stattdessen verwendet.

Sonarr und Radarr sehen die Qualität in der Newznab-Kategorie und im Titel (z.B. "Tatort S01E05 Der letzte Schrei 1080p"). Damit greifen die normalen Qualitätsprofile und -präferenzen.
