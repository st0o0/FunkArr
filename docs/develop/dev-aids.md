# Entwicklungshilfen

Werkzeuge im Repository, die die Arbeit an FunkArr leichter machen.

## In der laufenden App

- **Regelwerk-Debugger.** *Regelwerke → + Neu* in der Web-Oberfläche lässt deine Regeln gegen echte Mediathek-Daten laufen und zeigt, welche Einträge passen und warum. Er nutzt `TestScoringWorker` und bewertet damit genau wie eine echte Suche.
- **Bewertungsverlauf.** Jede Bewertung wird aufgezeichnet; der Verlauf eines Regelwerks (*Regelwerke → Regelwerk → Verlauf*) zeigt, welche Regel einen Eintrag angenommen oder abgelehnt hat.
- **Live-Logs.** Die *Einstellungen* streamen das Server-Log, für einen schnellen Blick brauchst du also kein `docker logs`.
- **Metriken.** `/metrics` liefert die Prometheus-Metriken aller Domänen, siehe [Observability](/observability).

## Dev-Builds

Setz das Label `dev-build` an einen Pull Request, um ein Container-Image davon nach `ghcr.io/st0o0/funkarr` zu veröffentlichen. So kannst du einen Branch ohne Release auf einem echten Server testen.

## Architekturmodell

`pnpm --dir docs exec likec4 serve architecture` öffnet das Modell mit allen Ansichten im LikeC4-Viewer. Die [VS-Code-Erweiterung](https://marketplace.visualstudio.com/items?itemName=likec4.likec4-vscode) bringt Vervollständigung und eine Live-Vorschau. Siehe [Architektur](./architecture#das-architekturmodell).

## KI-Agenten

Das Repository ist für Coding-Agenten eingerichtet, zum Mitmachen brauchst du aber keinen.

- `AGENTS.md` enthält die kompakten Regeln, `CLAUDE.md` ergänzt den OpenSpec-Ablauf und welcher Skill für welche Aufgabe geladen wird.
- `.claude/skills/` enthält Vorlagen für wiederkehrende Arbeit: `funkarr-actor`, `funkarr-message`, `funkarr-endpoint`, `funkarr-test`, die OpenSpec-Skills und `e2e-verify`, das den manuellen E2E-Plan gegen den Dev-Stack ausführt.
- Agenten folgen demselben OpenSpec-Ablauf wie Menschen, und ihre Pull Requests werden genauso geprüft.
