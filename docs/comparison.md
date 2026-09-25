# Vergleich mit Alternativen

Drei Projekte lösen dasselbe Problem: Inhalte aus deutschsprachigen öffentlich-rechtlichen Mediatheken in Sonarr und Radarr verfügbar machen. Alle drei nutzen die [MediathekViewWeb](https://mediathekviewweb.de/) API als Datenquelle und stellen eine Newznab-Indexer-API und eine SABnzbd-Download-Client-API bereit, sodass die *arr-Apps sie wie eine normale Usenet-Quelle behandeln.

::: info Gleiche Datenquelle
Alle drei Projekte nutzen dieselbe MediathekViewWeb API. Die verfügbaren Inhalte (ARD, ZDF, ORF, SRF usw.) sind identisch - der Unterschied liegt darin, wie jedes Projekt die Inhalte matched, bewertet und herunterlädt.
:::

## Kurzvergleich

|                        | FunkArr              | MediathekArr           | RundfunkArr             |
|------------------------|----------------------|------------------------|-------------------------|
| **Tech-Stack**         | .NET 10 / Akka.NET   | .NET (C#)              | Node.js / Next.js       |
| **Status**             | Aktiv                | Beta (letztes Release Feb 2025) | Aktiv (v1.3.0)   |
| **GitHub Stars**       | Neues Projekt        | ~374                   | ~40                     |
| **Sonarr**             | Ja                   | Ja                     | Ja                      |
| **Radarr**             | Ja                   | Eingeschränkt (WIP)    | Ja (seit v1.1.0)        |
| **Regelwerke**         | Community + eigene, Web-UI-Editor | Internes Matching | Community-Regelwerke, Auto-Update von GitHub |
| **Metadaten**          | TMDB + TVDB (erfordert API-Keys) | Nur TVDB    | Lokale shows.json → TVDB → TMDB |
| **Download**           | FFmpeg (HLS + direkt)| Direkter HTTP-Download | Direkter HTTP-Download + yt-dlp |
| **Ausgabe**            | MKV (Remux, keine Neucodierung) | MKV         | MKV (optionales FFmpeg) |
| **Untertitel**         | SRT aus HLS oder separater Download | Ja        | Ja                      |
| **Datenbank**          | SQLite oder PostgreSQL | SQLite                | SQLite (Prisma)         |
| **Web-UI**             | Ja (Vue.js, mit Setup-Assistent) | Ja (mit Setup-Assistent) | Ja (Next.js, mit Setup-Assistent) |
| **Port**               | 6969                 | 5007                   | 6767                    |
| **Docker**             | Multi-Arch (amd64, arm64, armv7) | Ja          | Multi-Arch (amd64, arm64) |
| **Auto-Konfiguration** | Ja (erstellt Indexer + Download-Client in Prowlarr/Sonarr/Radarr) | Ja (Setup-Assistent) | Nein |
| **Proxy-Support**      | Nein                 | Nein                   | Ja (für Downloads + yt-dlp) |
| **Umlaut-Behandlung**  | Eingebaut            | Via UmlautAdaptarr (separater Dienst) | Nicht dokumentiert |
| **Match-Verlauf**      | Ja (Diagnose + Statistiken) | Nein            | Nein                    |
| **PUID/PGID**          | Ja                   | Nicht dokumentiert      | Ja                      |
| **ORF/SRF**            | Via MediathekViewWeb | M3U-Download (beta.12) | HLS via yt-dlp (SRF braucht SRG-SSR-Credentials) |

## MediathekArr

[MediathekArr](https://github.com/PCJones/MediathekArr) von PCJones ist das populärste Projekt in diesem Bereich (~374 Stars). Es verwendet ein .NET-Backend und integriert MediathekViewWeb, UmlautAdaptarr und TheTVDB.

### Stärken

- **Größte Community** mit aktivem Discord-Kanal (UsenetDE Server) und Telegram - beste Anlaufstelle bei Problemen
- **[UmlautAdaptarr](https://github.com/PCJones/UmlautAdaptarr)** (~306 Stars) - Begleittool das Suchanfragen zwischen *arr-Apps und Indexern abfängt und modifiziert, um deutsches Umlaut-Matching, Titelerkennung und Release-Benennung in Sonarr, Lidarr und Readarr zu verbessern (Radarr-Support in Arbeit)
- **Setup-Assistent** im Web-Interface der durch die Ersteinrichtung führt
- **Erweitertes Filter- und Matching-System** für Serien, Staffeln und Episoden

### Einschränkungen

- Kein erweiterbares Regelwerk-System - Titel-Matching verwendet eingebaute Logik. Funktioniert gut für gängige Sendungen, kann aber von Nutzern nicht für Nischeninhalte erweitert werden.
- Radarr-Filmunterstützung ist eingeschränkt/WIP. Das README sagt: "You can find a few movies via interactive search, but not a lot."
- ORF- und SRF-Unterstützung wurde in beta.12 (Februar 2025) über M3U-Download hinzugefügt. Issue #77 fordert ORF standardmäßig zu deaktivieren wegen Geoblocking.
- Benötigt [UmlautAdaptarr](https://github.com/PCJones/UmlautAdaptarr) als separaten Begleitdienst für korrekte deutsche Titelauflösung.
- Noch in der Beta - das README warnt: "use the beta image until 1.0 is released. Latest/Main is not working." Letztes Release (beta.12) ist von Februar 2025. V2 wird in Issues geplant.
- Nur TVDB, keine TMDB-Integration.
- Nur SQLite, Datenbank nicht konfigurierbar.
- Downloads nur per direktem HTTP (kein HLS/FFmpeg).

## RundfunkArr

[RundfunkArr](https://github.com/rundfunkarr/rundfunkarr) (~40 Stars) ist ein Node.js/Next.js-Projekt. Hieß ursprünglich ebenfalls "MediathekArr" und wurde umbenannt um Verwechslungen zu vermeiden.

### Stärken

- **yt-dlp-Integration** für HLS-Stream-Auflösung - handhabt SRF- und ORF-Streams und unterstützt mehr Randfall-Formate als FFmpeg allein. Version-Pinning mit Checksum-Verifikation in Docker.
- **Proxy-Unterstützung** für yt-dlp und Downloads (nicht Metadaten-APIs) - nützlich für Zugriff auf geoblockierte Inhalte aus dem Ausland
- **Voller Radarr-Support** seit v1.1.0 mit TMDB/IMDB-ID-Unterstützung und intelligentem Titel-Parsing
- Aktive Entwicklung mit regelmäßigen Releases, null offene Issues und einem Setup-Assistenten
- PUID/PGID-Unterstützung und Multi-Arch-Docker-Images (amd64, arm64)
- Mehrere Metadatenquellen: lokale shows.json → TVDB → TMDB
- Community-Regelwerke mit Auto-Update von GitHub (seit v1.2.0)

### Einschränkungen

- Regelwerke werden per PR zum Repository gepflegt - kein Web-UI-Editor zum Erstellen oder Ändern von Regeln.
- Metadaten-Auflösung prüft zuerst eine lokale `shows.json`, die bei neuen Sendungen manuell aktualisiert werden muss, wenn sie nicht in TVDB/TMDB sind.
- SRF-Support erfordert Credentials vom SRG-SSR Developer-Portal.
- yt-dlp löst Video-Referenzen erst beim Download auf, nicht bei der Suche - regionale Einschränkungen hängen vom Proxy-Standort und der Verfügbarkeit beim Sender ab.
- Kein Scoring, Match-Verlauf oder Diagnose.
- Kleinere Community (~40 Stars, kein Discord/Telegram).

## FunkArr - Ehrliche Einschränkungen

Der Fairness halber hat FunkArr ebenfalls Einschränkungen:

- **Kein Proxy-Support** - anders als RundfunkArr gibt es keine Möglichkeit, einen Proxy für geoblockierte ORF/SRF-Inhalte zu nutzen.
- **Kein yt-dlp** - verwendet ausschließlich FFmpeg für HLS und direkte Downloads. Wenn ein Stream-Format nicht vom nativen FFmpeg-HLS-Demuxer unterstützt wird, schlägt es fehl. yt-dlp handhabt mehr Randfälle.
- **Jüngstes und kleinstes Projekt** - MediathekArr hat Jahre Vorsprung und die größte Nutzerbasis. Es gibt noch keinen Discord- oder Telegram-Kanal.
- **TVDB- und TMDB-API-Keys erforderlich** für Metadaten-Anreicherung - sie sind optional, aber für volle Funktionalität nötig.
- **Match-Verlauf ist diagnostisch, nicht adaptiv** - die Scoring-Engine führt Regeln deterministisch aus. Der Verlauf zeichnet vergangene Durchläufe auf und aggregiert Statistiken zur Fehlersuche, aber er lernt nicht und passt keine Gewichtungen an.

## Welches Projekt für welchen Einsatz?

- **MediathekArr** wenn du hauptsächlich ARD/ZDF-Inhalte schaust und die größte Community für Support willst.
- **RundfunkArr** wenn du Proxy-Unterstützung für geoblockierte Inhalte brauchst (SRF/ORF aus dem Ausland), vollen Radarr-Filmsupport willst oder yt-dlps breitere Formatunterstützung nutzen möchtest.
- **FunkArr** wenn du Community-getriebene Regelwerke mit einem visuellen Web-Editor willst, Match-Scoring-Diagnose, PostgreSQL-Unterstützung oder TMDB-Metadaten neben TVDB.
