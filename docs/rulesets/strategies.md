# Matching-Strategien

Jede Regel hat eine **Strategie**, die bestimmt, wie FunkArr Staffel-/Episodeninformationen aus einem Mediathek-Eintrag extrahiert. Die Wahl der richtigen Strategie hängt davon ab, wie die Titel der Sendung strukturiert sind.

## Strategieauswahl

| Strategie | Verwende wenn... | Beispielsendungen |
|---|---|---|
| `seasonAndEpisodeNumber` | Titel Staffel- und Episodennummern enthalten, auch in nicht-standardmäßigen Formaten | Sendungen mit „Staffel 3 Folge 12" oder „S03E12" im Titel |
| `byAbsoluteEpisodeNumber` | Episoden eine einzelne fortlaufende Nummer ohne Staffeln verwenden | Schloss Einstein (1000+ Episoden), Daily Soaps |
| `itemTitleExact` | Episodentitel einzigartig sind und exakt übereinstimmen müssen | Anthologie-Serien mit eigenständigen Titeln pro Episode |
| `itemTitleIncludes` | Episodentitel in einem längeren String eingebettet sind | Tatort („Tatort: Borowski und die Kinder") |
| `itemTitleEqualsAirdate` | Episoden durch ihr Ausstrahlungsdatum identifiziert werden | heute-show, Die Sendung mit der Maus, Talkshows |

## seasonAndEpisodeNumber

Extrahiert eine Staffel- und Episodennummer aus dem Mediathek-Titel mittels Regex-Mustern.

**Wann verwenden:** Der Titel enthält Staffel- und Episodeninformationen in beliebigem Format - deutsch („Staffel 3 Folge 12"), abgekürzt („S03/E12") oder in Klammern eingebettet.

**Benötigte Felder:**
- `seasonRegex` - Muster zur Extraktion der Staffelnummer
- `episodeRegex` - Muster zur Extraktion der Episodennummer
- `captureGroup` (optional) - welche Capture Group verwendet wird (Standard: 1)

**Beispiel:**

Für eine Sendung mit Titeln wie „Feuer und Flamme (S03/E05)":

```json
{
  "id": "regex-se",
  "priority": 0,
  "strategy": "seasonAndEpisodeNumber",
  "seasonRegex": "S(\\d{2})",
  "episodeRegex": "E(\\d{2})",
  "filters": {
    "all": [
      { "field": "duration", "op": "greaterThan", "value": "20" }
    ]
  }
}
```

Der `seasonRegex` erfasst `03` aus „S03", und der `episodeRegex` erfasst `05` aus „E05".

::: tip
Der Regex muss eine Capture Group `()` enthalten. FunkArr extrahiert standardmäßig den Inhalt der ersten Capture Group. Verwende `captureGroup`, um eine andere Gruppe auszuwählen.
:::

## byAbsoluteEpisodeNumber

Extrahiert eine einzelne absolute Episodennummer ohne Staffel. Sonarr ordnet sie intern über seinen Episodenguide der richtigen Staffel zu.

**Wann verwenden:** Langlaufende Sendungen, die einen einzelnen fortlaufenden Episodenzähler verwenden. Häufig bei deutschen Kindersendungen und täglichen Serien.

**Benötigte Felder:**
- `episodeRegex` - Muster zur Extraktion der Episodennummer
- `captureGroup` (optional)

**Beispiel: Schloss Einstein**

Mediathek-Titel: „Schloss Einstein (1042)", „Schloss Einstein (Folge 1043)"

```json
{
  "id": "absolute-episode",
  "priority": 0,
  "filters": {
    "all": [
      { "field": "duration", "op": "greaterThan", "value": "15" }
    ]
  },
  "strategy": "byAbsoluteEpisodeNumber",
  "episodeRegex": "\\((\\d{3,5})\\)"
}
```

Der Regex `\((\d{3,5})\)` matcht eine 3-bis-5-stellige Zahl in Klammern und extrahiert sie. Für „Schloss Einstein (1042)" ist die extrahierte Episodennummer `1042`.

Eine zweite Regel mit niedrigerer Priorität behandelt die „Folge"-Variante:

```json
{
  "id": "absolute-episode-p1",
  "priority": 1,
  "strategy": "byAbsoluteEpisodeNumber",
  "episodeRegex": "\\(Folge (\\d{3,5})\\)"
}
```

## itemTitleExact

Konstruiert einen Titel-String aus Teilen und gleicht ihn **exakt** mit dem Sonarr/Radarr-Episodentitel ab. Der konstruierte Titel muss exakt übereinstimmen.

**Wann verwenden:** Episodentitel sind einzigartig und vorhersagbar. Der Mediathek-Titel enthält den Episodentitel wörtlich oder er kann sauber extrahiert werden.

**Benötigte Felder:**
- `titleRules` - Array von Titelteilen (fester Text und/oder Regex-Extraktionen)

**Beispiel:**

Für eine Sendung, bei der der Mediathek-Titel exakt dem Episodentitel entspricht:

```json
{
  "id": "title-exact",
  "priority": 0,
  "strategy": "itemTitleExact",
  "titleRules": [
    { "type": "regex", "field": "title", "pattern": "^(.+)$" }
  ]
}
```

## itemTitleIncludes

Konstruiert einen Titel-String aus Teilen und prüft, ob der Sonarr/Radarr-Episodentitel ihn als Substring **enthält**. Nachsichtiger als exaktes Matching.

**Wann verwenden:** Episodentitel sind im Mediathek-Titel mit zusätzlicher Formatierung eingebettet, oder wenn exaktes Matching zu fragil ist.

**Benötigte Felder:**
- `titleRules` - Array von Titelteilen

**Beispiel: Tatort**

Mediathek-Titel: „Tatort: Borowski und die Kinder", „Tatort: Der letzte Schrei"

```json
{
  "id": "title-includes",
  "priority": 0,
  "filters": {
    "all": [
      { "field": "duration", "op": "greaterThan", "value": "35" }
    ]
  },
  "strategy": "itemTitleIncludes",
  "titleRules": [
    { "type": "static", "value": " - " },
    {
      "type": "regex",
      "field": "title",
      "pattern": "(?<=Tatort:\\s*)\\S.*"
    }
  ]
}
```

**So funktioniert es:**

1. Der statische Teil hängt `" - "` an den konstruierten Titel an
2. Der Regex-Teil extrahiert alles nach „Tatort: " aus dem Mediathek-Titel
3. Ergebnis: `" - Borowski und die Kinder"`
4. Sonarr sucht nach einem Episodentitel, der diesen Substring enthält

Das Tatort-Regelwerk enthält eine zweite Regel mit Priorität 1 und einem toleranteren Regex für Sonderfälle, in denen das Titelformat abweicht.

**Beispiel: Film-Matching (Apocalypse Now)**

```json
{
  "id": "title-match",
  "priority": 0,
  "filters": {
    "all": [
      { "field": "topic", "op": "contains", "value": "Spielfilm" }
    ]
  },
  "strategy": "itemTitleIncludes",
  "titleRules": [
    { "type": "static", "value": "Apocalypse Now" }
  ]
}
```

Dies matcht jeden Eintrag im „Spielfilm"-Thema, der „Apocalypse Now" im Titel enthält.

## itemTitleEqualsAirdate

Extrahiert ein Ausstrahlungsdatum aus dem Titel. Verwendet für Sendungen, die über ihr Ausstrahlungsdatum statt Staffel-/Episodennummern identifiziert werden.

**Wann verwenden:** Talkshows, Nachrichtensendungen, Wochenmagazine und tägliche Sendungen, bei denen Episoden über das Ausstrahlungsdatum identifiziert werden.

**Benötigte Felder:**
- `titleRules` - Regex zur Extraktion des Datums aus dem Mediathek-Titel

**Beispiel: heute-show**

Mediathek-Titel: „heute-show vom 13. September 2026"

```json
{
  "id": "airdate",
  "priority": 0,
  "filters": {
    "all": [
      { "field": "duration", "op": "greaterThan", "value": "25" }
    ]
  },
  "strategy": "itemTitleEqualsAirdate",
  "titleRules": [
    {
      "type": "regex",
      "field": "title",
      "pattern": "^heute-show vom (\\d{1,2}\\. \\w+ \\d{4})"
    }
  ]
}
```

Der Regex extrahiert „13. September 2026" aus dem Titel. FunkArr parst dies in ein Datum und ordnet es der Episode mit diesem Ausstrahlungsdatum in Sonarr zu.

**Beispiel: Die Sendung mit der Maus**

Mediathek-Titel verwenden ein numerisches Datumsformat: „Die Sendung mit der Maus 15.09.2026"

```json
{
  "id": "airdate",
  "priority": 0,
  "filters": {
    "all": [
      { "field": "duration", "op": "greaterThan", "value": "21" }
    ]
  },
  "strategy": "itemTitleEqualsAirdate",
  "titleRules": [
    {
      "type": "regex",
      "field": "title",
      "pattern": "\\b\\d{2}\\.\\d{2}\\.\\d{4}\\b"
    }
  ]
}
```

::: tip
Für datumsbasierte Sendungen setze den **Serientyp** in Sonarr auf **Daily**. Ohne dies sucht Sonarr nicht nach Episoden anhand des Datums.
:::

## Titelkonstruktion im Detail

Titelregeln (`titleRules`) bauen einen String auf, indem sie Teile von links nach rechts anhängen. Jeder Teil ist entweder:

- **Statisch** - hängt festen Text an
- **Regex** - führt einen Regex gegen ein Mediathek-Feld aus und hängt den Match an

### Auswertungsreihenfolge

```
Teil 1        Teil 2        Teil 3
[statisch] → [regex]    → [statisch] → fertiger Titel
" - "        "Borowski"    ""            " - Borowski"
```

### Regex-Teile

Ein Regex-Teil hat drei Komponenten:
1. `field` - welches Mediathek-Feld durchsucht wird (normalerweise `title`)
2. `pattern` - der auszuführende Regex
3. `captureGroup` - welche Gruppe extrahiert wird (Standard: 0 = vollständiger Match)

Wenn der Regex nicht matcht, scheitert die gesamte Regel bei der Identifikation.

### Mehrere Regeln für Robustheit

Reale Mediathek-Titel variieren. Ein gängiges Muster ist, mehrere Regeln mit aufsteigender Priorität zu definieren - die erste behandelt den Standardfall, Fallback-Regeln behandeln Sonderfälle:

```json
"rules": [
  {
    "id": "main",
    "priority": 0,
    "strategy": "itemTitleIncludes",
    "titleRules": [{ "type": "regex", "field": "title", "pattern": "striktes-muster" }]
  },
  {
    "id": "fallback",
    "priority": 1,
    "strategy": "itemTitleIncludes",
    "titleRules": [{ "type": "regex", "field": "title", "pattern": "tolerantes-muster" }]
  }
]
```

Priorität `0` wird zuerst versucht. Wenn sie fehlschlägt (Regex matcht nicht oder Filter lehnt den Kandidaten ab), bekommt Priorität `1` eine Chance.
