# Matching Strategies

Every rule has a **strategy** that determines how FunkArr extracts season/episode information from a Mediathek entry. Choosing the right strategy depends on how the show's titles are structured.

## Choosing a Strategy

| Strategy | Use when... | Example shows |
|---|---|---|
| `seasonAndEpisodeNumber` | Titles contain season and episode numbers, even in non-standard formats | Shows with "Staffel 3 Folge 12" or "S03E12" in the title |
| `byAbsoluteEpisodeNumber` | Episodes use a single running number with no seasons | Schloss Einstein (1000+ episodes), daily soaps |
| `itemTitleExact` | Episode titles are unique and must match exactly | Anthology series with distinct titles per episode |
| `itemTitleIncludes` | Episode titles are embedded in a longer string | Tatort ("Tatort: Borowski und die Kinder") |
| `itemTitleEqualsAirdate` | Episodes are identified by their broadcast date | heute-show, Die Sendung mit der Maus, talk shows |

## seasonAndEpisodeNumber

Extracts a season and episode number from the Mediathek title using regex patterns.

**When to use:** The title contains season and episode information in any format - German ("Staffel 3 Folge 12"), abbreviated ("S03/E12"), or embedded in parentheses.

**Required fields:**
- `seasonRegex` - pattern to extract the season number
- `episodeRegex` - pattern to extract the episode number
- `captureGroup` (optional) - which capture group to use (default: 1)

**Example:**

For a show where titles look like "Feuer und Flamme (S03/E05)":

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

The `seasonRegex` captures `03` from "S03", and the `episodeRegex` captures `05` from "E05".

::: tip
The regex must contain a capture group `()`. FunkArr extracts the content of the first capture group by default. Use `captureGroup` to select a different group.
:::

## byAbsoluteEpisodeNumber

Extracts a single absolute episode number with no season. Sonarr maps it to the correct season internally using its episode guide.

**When to use:** Long-running shows that use a single running episode counter. Common for German children's shows and daily series.

**Required fields:**
- `episodeRegex` - pattern to extract the episode number
- `captureGroup` (optional)

**Example: Schloss Einstein**

Mediathek titles: "Schloss Einstein (1042)", "Schloss Einstein (Folge 1043)"

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

The regex `\((\d{3,5})\)` matches a 3-to-5-digit number inside parentheses and extracts it. For "Schloss Einstein (1042)", the extracted episode number is `1042`.

A second rule with lower priority handles the "Folge" variant:

```json
{
  "id": "absolute-episode-p1",
  "priority": 1,
  "strategy": "byAbsoluteEpisodeNumber",
  "episodeRegex": "\\(Folge (\\d{3,5})\\)"
}
```

## itemTitleExact

Constructs a title string from parts and matches it **exactly** against the Sonarr/Radarr episode title. The constructed title must be an exact match.

**When to use:** Episode titles are unique and predictable. The Mediathek title contains the episode title verbatim or can be extracted cleanly.

**Required fields:**
- `titleRules` - array of title parts (static text and/or regex extractions)

**Example:**

For a show where the Mediathek title is exactly the episode title:

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

Constructs a title string from parts and checks if the Sonarr/Radarr episode title **contains** it as a substring. More forgiving than exact matching.

**When to use:** Episode titles are embedded in the Mediathek title with extra formatting, or when exact matching is too brittle.

**Required fields:**
- `titleRules` - array of title parts

**Example: Tatort**

Mediathek titles: "Tatort: Borowski und die Kinder", "Tatort: Der letzte Schrei"

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

**How it works:**

1. The static part appends `" - "` to the constructed title
2. The regex part extracts everything after "Tatort: " from the Mediathek title
3. Result: `" - Borowski und die Kinder"`
4. Sonarr searches for an episode title that contains this substring

The Tatort ruleset includes a second rule at priority 1 with a more lenient regex for edge cases where the title format varies.

**Example: Movie matching (Apocalypse Now)**

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

This matches any entry in the "Spielfilm" topic that contains "Apocalypse Now" in its title.

## itemTitleEqualsAirdate

Extracts a broadcast date from the title. Used for shows identified by their air date rather than season/episode numbers.

**When to use:** Talk shows, news programs, weekly magazines, and daily shows where episodes are identified by broadcast date.

**Required fields:**
- `titleRules` - regex to extract the date from the Mediathek title

**Example: heute-show**

Mediathek titles: "heute-show vom 13. September 2026"

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

The regex extracts "13. September 2026" from the title. FunkArr parses this into a date and matches it to the episode with that air date in Sonarr.

**Example: Die Sendung mit der Maus**

Mediathek titles use a numeric date format: "Die Sendung mit der Maus 15.09.2026"

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
For airdate-matched shows, set the **Series Type** to **Daily** in Sonarr. Without this, Sonarr won't look up episodes by date.
:::

## Title Construction Deep-Dive

Title rules (`titleRules`) build a string by appending parts left to right. Each part is either:

- **Static** - appends literal text
- **Regex** - runs a regex against a Mediathek field and appends the match

### Evaluation order

```
Part 1        Part 2        Part 3
[static]  →  [regex]   →  [static]  →  final title
" - "        "Borowski"    ""            " - Borowski"
```

### Regex parts

A regex part has three components:
1. `field` - which Mediathek field to search (usually `title`)
2. `pattern` - the regex to run
3. `captureGroup` - which group to extract (default: 0 = full match)

If the regex doesn't match, the entire rule fails identification.

### Multiple rules for robustness

Real-world Mediathek titles vary. A common pattern is to define multiple rules at increasing priority levels - the first handles the standard case, and fallback rules handle edge cases:

```json
"rules": [
  {
    "id": "main",
    "priority": 0,
    "strategy": "itemTitleIncludes",
    "titleRules": [{ "type": "regex", "field": "title", "pattern": "strict-pattern" }]
  },
  {
    "id": "fallback",
    "priority": 1,
    "strategy": "itemTitleIncludes",
    "titleRules": [{ "type": "regex", "field": "title", "pattern": "lenient-pattern" }]
  }
]
```

Priority `0` is tried first. If it fails (regex doesn't match or filter rejects the candidate), priority `1` gets a chance.
