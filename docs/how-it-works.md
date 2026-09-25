# So funktioniert FunkArr

Diese Seite erklärt, wie FunkArr intern arbeitet: vom Eingang einer Suchanfrage über das Scoring und die Metadaten-Anreicherung bis zum fertigen Download.

## Ablauf einer Suche

Wenn Sonarr oder Radarr eine Suchanfrage an FunkArr senden, durchläuft diese mehrere Stufen:

### 1. Newznab-API empfängt die Anfrage

Sonarr/Radarr senden eine Standard-Newznab-Anfrage an `/index/api`. FunkArr erkennt den Typ anhand des Parameters:

- `t=tvsearch` - Seriensuche (von Sonarr), mit optionaler TVDB-ID, Staffel und Episode
- `t=movie` - Filmsuche (von Radarr), mit optionaler IMDB-ID oder TMDB-ID
- `t=search` - Allgemeine Suche (von Prowlarr), Kategorie bestimmt den Typ

Aus der Anfrage wird ein interner Suchbefehl erstellt und an den SearchManager weitergeleitet.

### 2. SearchManager koordiniert die Suche

Der SearchManager entscheidet anhand der Kategorie, ob Serien, Filme oder beides gesucht wird:

- Kategorie 5000-5999: nur Serien
- Kategorie 2000-2999: nur Filme
- Keine Kategorie: beides parallel, Ergebnisse werden zusammengeführt

Jede Suche hat ein Timeout von 30 Sekunden. Wenn bei einer kombinierten Suche ein Teil fehlschlägt, werden die Ergebnisse des anderen Teils trotzdem zurückgegeben.

### 3. MediathekViewWeb-Abfrage

Der MediathekViewWebManager sendet die Suchanfrage an die MediathekViewWeb-API. Diese durchsucht die Mediathek-Datenbank (ARD, ZDF, ORF, SRF und weitere Sender).

Suchanfragen können nach Topic (Sendungsname), Titel und Beschreibung filtern. Die Ergebnisse enthalten für jeden Eintrag:

- Sender, Thema, Titel, Beschreibung
- Video-URLs in verschiedenen Qualitäten (HD, normal, niedrig)
- Untertitel-URL (falls verfügbar)
- Dauer, Größe, Ausstrahlungszeitpunkt

FunkArr begrenzt die gleichzeitigen Abfragen an MediathekViewWeb auf 3, um die API nicht zu überlasten. Weitere Anfragen werden automatisch in eine Warteschlange gestellt.

### 4. Regelwerk-Zuordnung (RuleSet Matching)

Wenn eine TVDB-ID oder IMDB-ID in der Anfrage enthalten ist, lädt FunkArr das passende Regelwerk aus dem RuleSet-Store. Das Regelwerk bestimmt, wie Mediathek-Titel in strukturierte Staffel-/Episodenformate umgewandelt werden.

Ist kein Regelwerk vorhanden oder keine ID angegeben, werden die Mediathek-Ergebnisse direkt an das Scoring weitergeleitet.

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

Das Scoring läuft parallel über einen Pool von Worker-Aktoren. Die Poolgröße ist konfigurierbar über `FunkArr__Scoring__PoolSize` (Standard: 4).

### 6. Metadaten-Anreicherung

Nach dem Scoring werden die Treffer mit externen Metadaten angereichert (siehe Abschnitt [Metadaten-Anreicherung](#metadaten-anreicherung)).

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

Der DownloadManager verwaltet eine persistente Warteschlange mit folgenden Funktionen:

- **Parallele Downloads:** Standardmäßig werden bis zu 3 Downloads gleichzeitig ausgeführt (konfigurierbar über `FunkArr__Download__ConcurrentDownloads`).
- **Prioritäten:** Jeder Download hat eine Priorität. Downloads mit höherer Priorität werden bevorzugt abgearbeitet.
- **Pausieren/Fortsetzen:** Die gesamte Warteschlange kann pausiert und wieder fortgesetzt werden.
- **Force-Start:** Einzelne Downloads können sofort gestartet werden, auch wenn die maximale Anzahl paralleler Downloads bereits erreicht ist.
- **Umordnen:** Downloads können an eine bestimmte Position in der Warteschlange verschoben oder miteinander getauscht werden.
- **Löschen:** Wartende oder aktive Downloads können aus der Warteschlange entfernt werden.
- **Wiederholen:** Fehlgeschlagene Downloads können erneut in die Warteschlange eingereiht werden.

Die Warteschlange ist persistent - sie überlebt Neustart des Containers. Aktive Downloads werden nach einem Neustart automatisch erneut gestartet.

### Download-Zeitplan

FunkArr unterstützt optionale Download-Zeitfenster. Wenn konfiguriert, werden Downloads nur während der definierten Zeitfenster gestartet. Außerhalb der Fenster bleiben sie in der Warteschlange. Neue Downloads werden automatisch gestartet, sobald das nächste Zeitfenster beginnt.

### FFmpeg-Remux

Jeder Download wird von einem DownloadWorker verarbeitet:

1. **Untertitel vorbereiten:** Wenn eine Untertitel-URL vorhanden ist, wird die Untertiteldatei heruntergeladen. FunkArr erkennt das Format automatisch:
   - **TTML/XML** - wird nach SRT konvertiert (über einen eigenen TTML-zu-SRT-Konverter)
   - **WebVTT** - wird direkt als VTT-Datei verwendet
   - **SRT** - wird direkt verwendet

2. **Video herunterladen und remuxen:** FFmpeg lädt das Video herunter (direkte URL oder HLS-Stream) und verpackt es als MKV-Container. Wenn fur den Sender eine Netzwerk-Route mit Proxy konfiguriert ist, werden sowohl der Untertitel-Download als auch FFmpeg uber den konfigurierten HTTP-Proxy geroutet (siehe [Konfiguration - Netzwerk-Routen](/configuration#netzwerk-routen)):
   - Video- und Audio-Codecs werden kopiert (keine Neucodierung)
   - Untertitel werden als SRT-Spur mit deutschem Sprach-Tag (`language=deu`) eingebettet
   - Der Fortschritt wird live ausgelesen (heruntergeladene Bytes, Zeitposition, Geschwindigkeit)

3. **Fertigstellung:** Die fertige Datei wird vom `incomplete/`-Verzeichnis in das `complete/`-Verzeichnis verschoben, organisiert nach Kategorie-Unterverzeichnis (z.B. `complete/tv/`).

### Download-Verlauf

Jeder abgeschlossene Download (erfolgreich oder fehlgeschlagen) wird im Download-Verlauf aufgezeichnet. Der Verlauf speichert Titel, Kategorie, Dateigröße, Status, Fehlermeldung (bei Fehler), Download-Dauer und Abschlusszeitpunkt. Du kannst Verlaufseinträge einzeln entfernen.

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

Jede Suchanfrage, die über ein Regelwerk ausgewertet wird, wird im Scoring-Verlauf des jeweiligen Regelwerks gespeichert. Pro Snapshot werden aufgezeichnet:

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
| `FunkArr__MatchHistory__MaxSnapshots` | `100` | Maximale Anzahl gespeicherter Snapshots pro Regelwerk |
| `FunkArr__MatchHistory__MaxAgeDays` | `30` | Snapshots älter als diese Anzahl Tage werden gelöscht |
| `FunkArr__MatchHistory__SnapshotInterval` | `20` | Intervall zwischen Snapshot-Persistierungen |

## Metadaten-Anreicherung

FunkArr kann Suchergebnisse mit externen Metadaten anreichern, um Staffel- und Episodennummern zu bestimmen, wenn das Regelwerk allein nicht ausreicht.

### TVDB (Serien)

Für Serien lädt FunkArr die Episodenliste von TVDB und versucht, Mediathek-Einträge den richtigen Episoden zuzuordnen. Dabei werden zwei Methoden nacheinander versucht:

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
