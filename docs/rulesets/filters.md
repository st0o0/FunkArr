# Filter

Filter entscheiden, welche Mediathek-Einträge in eine Matching-Regel gelangen. Sie laufen **vor** der Identifikation — wenn ein Filter fehlschlägt, wird die Regel übersprungen und die nächste Regel in der Prioritätsreihenfolge versucht.

## Funktionsweise

Jede Regel kann ein `filters`-Objekt mit drei Abschnitten haben, die als boolesche Logik-Gatter funktionieren:

| Abschnitt | Logik | Verhalten |
|---|---|---|
| `all` | **UND** | Jede Bedingung muss zutreffen |
| `any` | **ODER** | Mindestens eine Bedingung muss zutreffen |
| `not` | **NICHT** | Keine der Bedingungen darf zutreffen |

Alle drei Abschnitte sind optional. Wenn `filters` ganz weggelassen wird, akzeptiert die Regel jeden Kandidaten.

Wenn mehrere Abschnitte vorhanden sind, werden sie in der Reihenfolge `all` → `any` → `not` ausgewertet. Alle Abschnitte müssen bestehen, damit der Filter erfolgreich ist.

## Felder

Jede Bedingung prüft ein Feld des Mediathek-Eintrags:

| Feld | Typ | Beschreibung | Beispielwert |
|---|---|---|---|
| `title` | string | Der vollständige Titel des Eintrags | „Tatort: Borowski und die Kinder" |
| `topic` | string | Der Themen-/Sendungsname | „Tatort" |
| `channel` | string | Der Sender | „ARD", „ZDF", „ORF", „SRF" |
| `description` | string | Beschreibungstext des Eintrags | „Kommissar Borowski ermittelt..." |
| `duration` | number | Dauer in Minuten | 90 |
| `timestamp` | number | Ausstrahlungszeitpunkt (Unix-Epoche) | 1726000000 |

## Operatoren

| Operator | Anwendung | Beschreibung |
|---|---|---|
| `eq` | string | Exakter String-Vergleich |
| `contains` | string | Substring-Match (Groß-/Kleinschreibung beachten) |
| `notContains` | string | Substring darf nicht enthalten sein |
| `greaterThan` | number | Numerischer Größer-als-Vergleich |
| `lessThan` | number | Numerischer Kleiner-als-Vergleich |
| `regex` | string | Regulärer-Ausdruck-Match |

::: warning
Numerische Werte müssen in JSON als Strings übergeben werden: `"value": "60"`, nicht `"value": 60`.
:::

## Gängige Muster

### Kurze Clips ausschließen

Die meisten Mediathek-Einträge enthalten Trailer, Teaser und Clip-Ausschnitte, die deutlich kürzer als vollständige Episoden sind. Filtere sie mit einer Dauerprüfung heraus:

```json
"filters": {
  "all": [
    { "field": "duration", "op": "greaterThan", "value": "25" }
  ]
}
```

Das ist der häufigste Filter — fast jedes Community-Regelwerk verwendet ihn.

### Auf einen bestimmten Sender beschränken

Wenn eine Sendung auf mehreren Sendern mit unterschiedlichen Titelformaten läuft, filtere nach Sender:

```json
"filters": {
  "all": [
    { "field": "channel", "op": "eq", "value": "ZDF" }
  ]
}
```

### Trailer und Sondersendungen ausschließen

Filtere Einträge mit Schlüsselwörtern heraus, die auf Nicht-Episoden-Inhalte hinweisen:

```json
"filters": {
  "not": [
    { "field": "title", "op": "contains", "value": "Trailer" },
    { "field": "title", "op": "contains", "value": "Vorschau" },
    { "field": "title", "op": "contains", "value": "(Audiodeskription)" }
  ]
}
```

Der `not`-Abschnitt lehnt jeden Eintrag ab, der **eine** seiner Bedingungen erfüllt.

### Nach Beschreibungs-Schlüsselwort filtern

Manche Sendungen teilen sich einen Themennamen, haben aber unterschiedliche Inhalte. Verwende die Beschreibung zur Unterscheidung:

```json
"filters": {
  "all": [
    { "field": "description", "op": "contains", "value": "Krimi" }
  ]
}
```

### Nach Titelmuster filtern

Verwende einen Regex-Filter, wenn die Titelstruktur relevant ist:

```json
"filters": {
  "all": [
    { "field": "title", "op": "regex", "value": "^Tatort:\\s" }
  ]
}
```

### Bedingungen kombinieren

Kombiniere Dauer- und Senderfilter für Präzision:

```json
"filters": {
  "all": [
    { "field": "duration", "op": "greaterThan", "value": "35" },
    { "field": "channel", "op": "eq", "value": "ARD" }
  ],
  "not": [
    { "field": "title", "op": "contains", "value": "Trailer" }
  ]
}
```

Dies matcht nur ARD-Einträge, die länger als 35 Minuten sind und keine Trailer sind.

## Verschachtelte Gruppen

Für komplexe Logik können Filterknoten verschachtelte Gruppen statt einfacher Bedingungen sein. Jede verschachtelte Gruppe hat ihre eigenen `all`/`any`/`not`-Abschnitte.

**Beispiel:** Einträge von ARD oder ZDF matchen, aber nur wenn sie länger als 30 Minuten sind:

```json
"filters": {
  "all": [
    { "field": "duration", "op": "greaterThan", "value": "30" },
    {
      "any": [
        { "field": "channel", "op": "eq", "value": "ARD" },
        { "field": "channel", "op": "eq", "value": "ZDF" }
      ]
    }
  ]
}
```

Das äußere `all` verlangt, dass beide Bedingungen erfüllt sind: Dauer > 30 UND (Sender = ARD ODER Sender = ZDF).

::: tip
Die meisten Regelwerke brauchen nur flache Bedingungen. Greife zu verschachtelten Gruppen, wenn du ODER-Logik innerhalb eines UND-Blocks brauchst, oder andere Kombinationen, die flache Abschnitte nicht ausdrücken können.
:::

## Short-Circuit-Auswertung

Filter werden mit Short-Circuit-Logik ausgewertet:

- **`all`**: stoppt bei der ersten fehlschlagenden Bedingung (verbleibende Bedingungen werden im Debugger als „Übersprungen" angezeigt)
- **`any`**: stoppt bei der ersten bestehenden Bedingung
- **`not`**: stoppt bei der ersten zutreffenden Bedingung (was bedeutet, dass der Filter fehlschlägt)

Dies ist im Pipeline-Trace des Debuggers sichtbar — übersprungene Bedingungen erscheinen in Grau.
