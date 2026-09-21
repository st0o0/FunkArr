# Eigene Regelwerke

Erstelle eigene Regelwerke in FunkArrs Web-Oberfläche, um Sendungen oder Filme abzudecken, die nicht im Community-Katalog enthalten sind, oder um Community-Regeln mit eigener Matching-Logik zu überschreiben.

## Dein erstes Regelwerk

Diese Anleitung erstellt ein Regelwerk für eine TV-Sendung namens „Abenteuer Wald", die auf SWR läuft.

### 1. Builder öffnen

Navigiere zu **Regelwerke → + Neu** in der FunkArr-Oberfläche. Der Builder öffnet sich mit einem leeren Formular links und dem Live-Vorschau-Panel rechts.

### 2. Identität ausfüllen

| Feld | Wert | Warum |
|---|---|---|
| **Regelwerk-ID** | `abenteuer-wald` | Eindeutiger kebab-case Bezeichner. Kann nach der Erstellung nicht geändert werden. |
| **Thema** | `Abenteuer Wald` | Exakt wie im Mediathek-Themenfeld angezeigt. |
| **Medientyp** | Serie | Teilt Sonarr/Radarr mit, wie es behandelt werden soll. |
| **Medienname** | `Abenteuer Wald` | Wird automatisch vom Thema übernommen. Ändere ihn nur, wenn die Sendung in TVDB/TMDB einen anderen Namen hat. |
| **TVDB ID** | `12345` | Auf thetvdb.com nachschlagen. Optional, aber empfohlen - Sonarr verwendet sie für die Zuordnung. |

Füge Aliase hinzu, wenn die Sendung unter verschiedenen Themennamen erscheint (z.B. „Abenteuer Wald - Spezial").

### 3. Standard-Konfidenz setzen

Setze die Konfidenz auf `1.0` für eine Sendung, bei der du zuverlässige Matches erwartest. Setze sie niedriger (z.B. `0.8`), wenn die Mediathek-Daten inkonsistent sind und Sonarr noch einmal prüfen soll.

### 4. Matching-Regel hinzufügen

Klicke auf **+ Regel hinzufügen**. Eine neue Regelkarte öffnet sich mit einer generierten ID.

| Feld | Wert |
|---|---|
| **Regel-ID** | `airdate` |
| **Priorität** | `0` (erste Regel, die versucht wird) |
| **Strategie** | `Titel entspricht Ausstrahlungsdatum` |

In diesem Beispiel sehen die Mediathek-Titel wie „Abenteuer Wald vom 13. September 2026" aus, sodass die Airdate-Strategie das Ausstrahlungsdatum direkt extrahiert.

### 5. Filter hinzufügen

Schließe kurze Clips aus, indem du einen Filter im **all**-Abschnitt hinzufügst:

- Feld: `duration`
- Operator: `greaterThan`
- Wert: `20`

Das stellt sicher, dass nur vollständige Episoden (länger als 20 Minuten) zugeordnet werden.

### 6. Mit dem Debugger testen

Das Live-Vorschau-Panel rechts lädt automatisch Mediathek-Einträge, die zu deinem Thema passen. Während du Regeln erstellst, erscheinen Treffer in Echtzeit mit grünen Indikatoren.

Für gründlicheres Testen klicke auf **Vollständiger Test**, um deine Regeln an den Server zu senden. Der vollständige Test zeigt einen detaillierten Pipeline-Trace für jeden Kandidaten - welche Regeln versucht wurden, welche Filter bestanden oder fehlgeschlagen sind und was extrahiert wurde.

### 7. Speichern

Klicke auf **Speichern**. FunkArr validiert dein Regelwerk gegen das JSON-Schema und registriert es sofort. Das neue Regelwerk erscheint in der Liste und beginnt mit dem Matching, wenn Sonarr oder Radarr das nächste Mal eine Suche starten.

## Überschreibungen

Lokale Regelwerke haben Vorrang vor Community-Regelwerken für dieselbe Sendung. Wenn beide existieren, führt FunkArr sie zusammen:

- Regeln mit **gleicher ID** → lokal ersetzt Community
- Regeln mit **neuer ID** → werden an die Liste angehängt
- **Aliase** → vereinigt (beide Sets kombiniert)
- **Media-Felder** → lokal gewinnt pro Feld (tvdbId, imdbId, etc.)
- **Konfidenz** → lokaler Wert wird verwendet, wenn gesetzt

Um ein Community-Regelwerk zu bearbeiten, navigiere zu seiner Detailseite und klicke auf **Bearbeiten**. Deine Änderungen werden als lokale Überschreibung gespeichert - die Community-Basis bleibt intakt und erhält weiterhin Updates.

## Standalone-Modus

Wenn du die Community-Basis komplett ignorieren möchtest, öffne deine lokale Regelwerk-JSON-Datei und setze `standalone: true`. Das weist FunkArr an, dein lokales Regelwerk unverändert zu verwenden, ohne Zusammenführung.

Verwende dies, wenn die Struktur eines Community-Regelwerks zu stark von dem abweicht, was du brauchst, oder wenn du die volle Kontrolle über jede Regel haben möchtest.

## Community-Regeln deaktivieren

Um bestimmte Community-Regeln zu überspringen, ohne sie zu ersetzen, füge ihre IDs zum `disable`-Array hinzu:

```json
{
  "disable": ["title-includes", "title-includes-p1"]
}
```

Deaktivierte Regeln werden beim Zusammenführen entfernt. Deine eigenen Regeln gelten weiterhin.

## Für Community exportieren

Wenn du ein Regelwerk erstellt hast, das für andere nützlich sein könnte, klicke auf **Für Community exportieren** auf der Detailseite. Das lädt eine bereinigte JSON-Datei herunter, die du als Pull Request im Community-Regelwerk-Repository einreichen kannst.

## Nächste Schritte

- [Feld-Referenz](./field-reference) - jedes Feld im Detail erklärt
- [Strategie-Guide](./strategies) - die richtige Identifikationsstrategie wählen
- [Filter-Kochbuch](./filters) - Kandidaten filtern und eingrenzen
