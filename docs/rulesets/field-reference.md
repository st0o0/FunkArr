# Feld-Referenz

Alle Felder im FunkArr-Regelwerk-JSON-Format, nach Abschnitt geordnet.

## Root-Felder

| Feld | Typ | Pflicht | Standard | Beschreibung |
|---|---|---|---|---|
| `topic` | string | Ja | - | Anzeigename der Sendung oder des Films. Muss exakt mit dem Mediathek-Themenfeld übereinstimmen. Wird für die themenbasierte Suche verwendet, wenn Sonarr/Radarr eine Suche starten. |
| `aliases` | string[] | Nein | `[]` | Alternative Themennamen. FunkArr prüft Aliase, wenn das primäre Thema nicht passt. Nützlich, wenn eine Sendung unter verschiedenen Namen bei verschiedenen Sendern erscheint (z.B. „Tatort aus Österreich"). |
| `media` | object | Ja | - | Externe Metadaten-IDs und Medientyp. Siehe [Media-Felder](#media-felder). |
| `confidence` | number | Nein | `0` | Standard-Konfidenzwert (0.0-1.0), der auf Regeln angewendet wird, die keinen eigenen setzen. Höhere Werte bedeuten, dass Sonarr/Radarr dem Match mehr vertrauen. Verwende `1.0` für zuverlässige Matches, niedrigere Werte für unsichere. |
| `rules` | array | Ja | - | Geordnete Liste von Matching-Regeln. Siehe [Regel-Felder](#regel-felder). |
| `standalone` | boolean | Nein | `false` | **Nur lokal.** Wenn `true`, wird das lokale Regelwerk unverändert verwendet - die Community-Basis wird komplett ignoriert. Kein Zusammenführen findet statt. |
| `disable` | string[] | Nein | `[]` | **Nur lokal.** Liste von Community-Regel-IDs, die beim Zusammenführen übersprungen werden. Die Regeln werden entfernt, bevor lokale Regeln angewendet werden. Jeder Wert muss einer Regel-`id` in der Community-Basis entsprechen. |

## Media-Felder

Das `media`-Objekt verknüpft ein Regelwerk mit externen Metadaten-Anbietern, damit Sonarr und Radarr die Sendung oder den Film identifizieren können.

| Feld | Typ | Pflicht | Standard | Beschreibung |
|---|---|---|---|---|
| `name` | string | Ja | - | Anzeigename für Sonarr/Radarr. Normalerweise identisch mit `topic`, kann aber abweichen (z.B. Topic „Checker Reportagen" → Media-Name „Checker Julian"). |
| `type` | string | Ja | `"show"` | Entweder `"show"` oder `"movie"`. Bestimmt, ob FunkArr Newznab-TV- oder Film-Kategorien generiert. |
| `tvdbId` | integer | Nein | - | TheTVDB Serien- oder Film-ID. Sonarr verwendet sie für die Zuordnung. Nachschlagen auf [thetvdb.com](https://thetvdb.com). |
| `imdbId` | string | Nein | - | IMDb-ID im `tt`-Format (z.B. `"tt0806910"`). Wird von Sonarr und Radarr verwendet. |
| `tmdbId` | integer | Nein | - | TheMovieDB-ID. Radarr verwendet sie für die Film-Zuordnung. |

::: tip
Mindestens eine externe ID (`tvdbId`, `imdbId` oder `tmdbId`) wird dringend empfohlen. Ohne sie kann Sonarr/Radarr den Download möglicherweise nicht der richtigen Sendung oder dem richtigen Film zuordnen.
:::

## Regel-Felder

Jeder Eintrag im `rules`-Array definiert einen Matching-Versuch. Regeln werden in `priority`-Reihenfolge ausgewertet - der erste erfolgreiche Match gewinnt.

| Feld | Typ | Pflicht | Standard | Beschreibung |
|---|---|---|---|---|
| `id` | string | Ja | - | Eindeutiger Bezeichner für diese Regel innerhalb des Regelwerks. Muss kebab-case sein (Kleinbuchstaben, Ziffern, Bindestriche, mindestens 3 Zeichen). Wird für die Überschreibung verwendet, wenn lokale Regeln Community-Regeln ersetzen. |
| `priority` | integer | Nein | `0` | Reihenfolge der Regelauswertung. Niedrigere Werte werden zuerst versucht. Bei gleicher Priorität gilt die Array-Reihenfolge. |
| `strategy` | string | Ja | - | Identifikationsstrategie. Bestimmt, wie Staffel-/Episodeninformationen extrahiert werden. Siehe [Strategien](./strategies) für Details. |
| `confidence` | number | Nein | - | Überschreibt die Standard-Konfidenz des Regelwerks für diese spezifische Regel. Verwende einen niedrigeren Wert für unsichere Regeln (z.B. einen Fallback-Regex). |
| `filters` | object | Nein | - | Vorfilterbedingungen, die bestehen müssen, bevor die Identifikation versucht wird. Siehe [Filter-Felder](#filter-felder). |
| `seasonRegex` | string | Nein | - | Regex-Muster zur Extraktion einer Staffelnummer. Verwendet mit der `seasonAndEpisodeNumber`-Strategie. Muss mindestens eine Capture Group enthalten. |
| `episodeRegex` | string | Nein | - | Regex-Muster zur Extraktion einer Episodennummer. Verwendet mit den Strategien `seasonAndEpisodeNumber` und `byAbsoluteEpisodeNumber`. Muss mindestens eine Capture Group enthalten. |
| `captureGroup` | integer | Nein | `1` | Welche Regex Capture Group für die Extraktion verwendet wird (0-indexiert). Standard ist die erste Capture Group. |
| `titleRules` | array | Nein | - | Titelkonstruktionsteile für die Strategien `itemTitleExact`, `itemTitleIncludes` und `itemTitleEqualsAirdate`. Siehe [Titelregel-Felder](#titelregel-felder). |

## Filter-Felder

Das `filters`-Objekt verwendet boolesche Logik, um einzugrenzen, welche Mediathek-Einträge in eine Regel gelangen. Filter laufen vor der Identifikation - wenn ein Filter fehlschlägt, wird die Regel übersprungen und die nächste Regel versucht.

### Filtergruppe

| Feld | Typ | Beschreibung |
|---|---|---|
| `all` | array | **UND** - jede Bedingung muss zutreffen. |
| `any` | array | **ODER** - mindestens eine Bedingung muss zutreffen. |
| `not` | array | **NICHT** - keine der Bedingungen darf zutreffen. |

Jedes Array enthält Filterknoten. Ein Knoten ist entweder eine **Bedingung** (Blatt) oder eine verschachtelte **Filtergruppe** (für komplexe Logik).

### Filterbedingung

| Feld | Typ | Pflicht | Beschreibung |
|---|---|---|---|
| `field` | string | Ja | Das zu prüfende Mediathek-Feld. Eines von: `title`, `topic`, `channel`, `description`, `duration`, `timestamp`. |
| `op` | string | Ja | Vergleichsoperator. Einer von: `eq`, `contains`, `notContains`, `greaterThan`, `lessThan`, `regex`. |
| `value` | string | Ja | Der Vergleichswert. Für numerische Operatoren (`greaterThan`, `lessThan`) wird die Zahl als String übergeben (z.B. `"60"`). |

### Filter-Felder-Referenz

| Feld | Typ | Beschreibung | Beispiel |
|---|---|---|---|
| `title` | string | Der Titel des Mediathek-Eintrags (z.B. „Tatort: Borowski und die Kinder") | `"Tatort:"` |
| `topic` | string | Der Themen-/Sendungsname (z.B. „Tatort") | `"Tatort"` |
| `channel` | string | Der Sender (z.B. „ARD", „ZDF", „ORF") | `"ARD"` |
| `description` | string | Beschreibungstext des Eintrags | `"Krimi"` |
| `duration` | number | Dauer in Minuten | `"60"` |
| `timestamp` | number | Ausstrahlungszeitpunkt (Unix-Epoche) | `"1726000000"` |

### Filter-Operatoren-Referenz

| Operator | Beschreibung | Beispiel |
|---|---|---|
| `eq` | Exakter String-Vergleich | `channel eq "ARD"` |
| `contains` | Substring-Match (Groß-/Kleinschreibung beachten) | `title contains "Tatort"` |
| `notContains` | Substring darf nicht enthalten sein | `title notContains "Trailer"` |
| `greaterThan` | Numerischer Größer-als-Vergleich | `duration greaterThan "25"` |
| `lessThan` | Numerischer Kleiner-als-Vergleich | `duration lessThan "120"` |
| `regex` | Regulärer-Ausdruck-Match | `title regex "S\\d{2}E\\d{2}"` |

Siehe das [Filter-Kochbuch](./filters) für gängige Muster und Beispiele.

## Titelregel-Felder

Titelregeln konstruieren oder extrahieren einen Titel-String Stück für Stück, von links nach rechts. Verwendet mit den Strategien `itemTitleExact`, `itemTitleIncludes` und `itemTitleEqualsAirdate`.

| Feld | Typ | Pflicht | Beschreibung |
|---|---|---|---|
| `type` | string | Ja | Entweder `"static"` (fester Text) oder `"regex"` (aus einem Mediathek-Feld extrahieren). |
| `value` | string | Nein | Der feste Text zum Anhängen. Verwendet wenn `type` gleich `"static"`. |
| `field` | string | Nein | Das Mediathek-Feld, gegen das der Regex ausgeführt wird. Verwendet wenn `type` gleich `"regex"`. Eines von: `title`, `topic`, `channel`, `description`. |
| `pattern` | string | Nein | Regex-Muster zum Abgleich. Verwendet wenn `type` gleich `"regex"`. Der gematchte Text (oder die angegebene Capture Group) wird an den konstruierten Titel angehängt. |
| `captureGroup` | integer | Nein | Welche Capture Group extrahiert wird (0 = vollständiger Match, 1 = erste Gruppe, etc.). Standard ist `0` (vollständiger Match) wenn weggelassen. |

### Beispiel

Dieses Titelregel-Set aus dem Tatort-Community-Regelwerk:

```json
"titleRules": [
  { "type": "static", "value": " - " },
  { "type": "regex", "field": "title", "pattern": "(?<=Tatort:\\s*)\\S.*" }
]
```

Für den Mediathek-Titel „Tatort: Borowski und die Kinder" ergibt dies: `" - Borowski und die Kinder"`. Mit der `itemTitleIncludes`-Strategie sucht Sonarr nach jedem Episodentitel, der diesen String enthält.
