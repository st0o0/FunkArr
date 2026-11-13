# Releases

Releases laufen automatisch über [release-please](https://github.com/googleapis/release-please). Niemand taggt von Hand.

```
Merge nach main ─▶ Release-PR aktualisiert ─▶ Release-PR gemergt ─▶ Tag + GitHub-Release
                                                                    ├─▶ Image ghcr.io/st0o0/funkarr
                                                                    └─▶ Doku-Deploy
```

1. Jeder Merge nach `main` aktualisiert einen Release-Pull-Request mit der nächsten Version und dem Changelog. Beides ergibt sich aus den Commit-Typen, siehe [Mitwirken](./#commits).
2. Wenn der Release-Pull-Request gemergt wird, entstehen Tag und Release, das Container-Image wird veröffentlicht und diese Seite neu ausgerollt.

Die Jobs kommen aus den gemeinsamen Workflows in [st0o0/github-workflows](https://github.com/st0o0/github-workflows) (`release-please.yml`, `docker-release.yml`, `docs.yml`).

## Zwei Komponenten

`release-please-config.json` definiert zwei unabhängige Komponenten:

| Komponente | Pfad | Tag |
|---|---|---|
| `funkarr` | Repository-Root | `funkarr-vX.Y.Z` |
| `rulesets` | `data/community/` | `rulesets-vX.Y.Z` |

Ein neues Regelwerk erscheint also ohne FunkArr-Release, siehe [Regelwerke](./rulesets).

## Versionierung

FunkArr ist bei 0.x. Ein `feat` erhöht die Patch-Version, eine inkompatible Änderung die Minor-Version. Inkompatible Änderungen brauchen keinen Migrationscode und keine Kompatibilitätsschichten, mit einer Ausnahme: persistierte Daten. Persistierte Enum-Werte und Datensätze bleiben lesbar, denn die Journale der Nutzer müssen ein Update überstehen.
