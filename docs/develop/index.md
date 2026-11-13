# Mitwirken

Schön, dass du helfen willst. Dieser Bereich erklärt, wie FunkArr gebaut, gestartet, getestet und veröffentlicht wird. Die Code-Konventionen stehen hier nicht noch einmal: Sie liegen auf [anvil](https://anvil.st0o0.net/), mit FunkArr als Referenzimplementierung.

## Wie du beitragen kannst

| Du willst … | Hier geht's los | C# nötig? |
|---|---|---|
| einen Fehler melden | [Fehlerbericht](https://github.com/st0o0/funkarr/issues/new?template=bug_report.yml) | Nein |
| eine Funktion vorschlagen | [Funktionswunsch](https://github.com/st0o0/funkarr/issues/new?template=feature_request.yml) | Nein |
| eine Serie oder einen Film anfragen | [Regelwerk-Anfrage](https://github.com/st0o0/funkarr/issues/new?template=ruleset_request.yml) | Nein |
| ein Regelwerk schreiben oder korrigieren | [Regelwerke](./rulesets) | Nein |
| die Doku verbessern | Seite bearbeiten (Link unten auf jeder Seite) | Nein |
| einen Fehler beheben oder eine Funktion bauen | [Lokale Entwicklung](./local-development), dann [Architektur](./architecture) | Ja |

## Wo die Regeln stehen

- **[anvil.st0o0.net](https://anvil.st0o0.net/)** erklärt die Konventionen mit Begründung und Beispielen: [C#](https://anvil.st0o0.net/csharp/coding-standards/), [Aktoren](https://anvil.st0o0.net/akka/actor-design/), [Persistenz](https://anvil.st0o0.net/akka/persistence/), [APIs](https://anvil.st0o0.net/api/minimal-api/), [Tests](https://anvil.st0o0.net/testing/assertions/), [Metriken](https://anvil.st0o0.net/observability/metrics/).
- **`AGENTS.md`** im Repository-Root ist die kompakte Fassung derselben Regeln, die KI-Agenten laden. Wenn du eine Konvention änderst, pass sie dort im selben Pull Request an.

## Eine Änderung planen

Größere Änderungen werden mit [OpenSpec](https://github.com/Fission-AI/OpenSpec) geplant, bevor Code entsteht. Das aktuelle Verhalten ist in `openspec/specs/` beschrieben, Vorschläge liegen in `openspec/changes/<name>/` (`proposal.md`, `design.md`, `specs/`, `tasks.md`).

1. **Explore**: Problem durchdenken, Code lesen, Optionen vergleichen.
2. **Propose**: Änderung schreiben und mit `openspec validate <name> --strict` prüfen.
3. **Apply**: Aufgaben umsetzen und abhaken.
4. **Archive**: Delta-Specs in `openspec/specs/` übernehmen und die Änderung nach `openspec/changes/archive/` verschieben.

Tippfehler, Dependency-Updates und offensichtliche Einzeiler brauchen keinen Vorschlag.

## Commits

[Conventional Commits](https://www.conventionalcommits.org/), geprüft von commitlint in der CI. Der Typ bestimmt, wo der Commit im Changelog landet:

| Typ | Changelog-Abschnitt |
|---|---|
| `feat` | Features (bei Regelwerken: New Rulesets) |
| `fix` | Bug Fixes (bei Regelwerken: Ruleset Fixes) |
| `perf`, `refactor`, `docs`, `deps` | Performance, Refactoring, Documentation, Dependencies |
| `test`, `ci`, `build`, `chore`, `style` | ausgeblendet |

Scopes sind optional; nimm die Domäne oder den Bereich, z. B. `fix(arrapi): …`.

## Vor dem Pull Request

```bash
dotnet build src/FunkArr.slnx
dotnet format src/FunkArr.slnx --verify-no-changes
dotnet run --project src/FunkArr.<Domain>.Tests/FunkArr.<Domain>.Tests.csproj
```

Der Build muss ohne Warnungen durchlaufen, und Verhaltensänderungen kommen mit Tests. Die vollständige Checkliste steht in der Pull-Request-Vorlage; was die CI prüft, steht unter [Bauen und testen](./building).

Code, Specs, Commits, Issues und Pull Requests sind auf Englisch. Die Doku gibt es auf Deutsch und Englisch, Doku-Änderungen gehören also in beide Sprachen.

Mit deinem Beitrag akzeptierst du den [Code of Conduct](https://github.com/st0o0/funkarr/blob/main/CODE_OF_CONDUCT.md). Sicherheitsprobleme meldest du über [SECURITY.md](https://github.com/st0o0/funkarr/blob/main/SECURITY.md), nicht in öffentlichen Issues.
