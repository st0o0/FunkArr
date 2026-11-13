# Bauen und testen

Alle Befehle laufen im Repository-Root.

## Voraussetzungen

| Werkzeug | Version |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | wie in `src/global.json` festgelegt |
| [Node.js](https://nodejs.org/) | 26, wie in CI und Dockerfile |
| [pnpm](https://pnpm.io/) | wie in `packageManager` der jeweiligen `package.json` |
| [Docker](https://docs.docker.com/get-docker/) mit Compose | aktuell |

## Backend

```bash
dotnet build src/FunkArr.slnx
dotnet format src/FunkArr.slnx --verify-no-changes
```

`TreatWarningsAsErrors` ist in jedem Projekt aktiv. Behebe die Ursache einer Warnung, statt sie zu unterdrücken. `dotnet format src/FunkArr.slnx` korrigiert die Formatierung.

Die Tests nutzen xUnit v3 auf Microsoft.Testing.Platform. Jedes Testprojekt ist ein Programm, also startest du es mit `dotnet run`, nicht mit `dotnet test`:

```bash
dotnet run --project src/FunkArr.Download.Tests/FunkArr.Download.Tests.csproj
dotnet run --project src/FunkArr.Download.Tests/FunkArr.Download.Tests.csproj -- --filter-method "*DispatchNext*"
```

Wie Tests geschrieben werden (TestKit, Probes, Assertions), steht auf anvil: [Aktor-Tests](https://anvil.st0o0.net/testing/actor-testing/), [Assertions](https://anvil.st0o0.net/testing/assertions/).

## Web-Oberfläche

```bash
pnpm --dir src/FunkArr.UI install
pnpm --dir src/FunkArr.UI build        # vue-tsc-Typprüfung und Vite-Build
pnpm --dir src/FunkArr.UI test         # Vitest
pnpm --dir src/FunkArr.UI test:ui      # Playwright (einmalig: exec playwright install chromium)
```

## Doku

```bash
pnpm --dir docs install
pnpm --dir docs dev
pnpm --dir docs build                  # schlägt auch bei toten Links fehl
```

## Was die CI prüft

| Check | Führt aus | Wann |
|---|---|---|
| `dotnet` | Build, Format-Prüfung, alle Testprojekte | jeder Pull Request |
| `frontend` | UI-Install und -Build | jeder Pull Request |
| `commits` | commitlint | jeder Pull Request |
| `UI Tests` | Typprüfung, Vitest, Playwright | Änderungen in `src/FunkArr.UI/` |
| `validate-rulesets` | JSON-Schema-Prüfung | Änderungen in `data/community/` |
| `Architecture model` | `likec4 validate` | Änderungen in `docs/architecture/` |
| `Security` | Abhängigkeits- und Image-Scans | Änderungen an Dockerfile oder Paketen, wöchentlich |

Build- und Test-Jobs kommen aus den gemeinsamen Workflows in [st0o0/github-workflows](https://github.com/st0o0/github-workflows), siehe [CI auf anvil](https://anvil.st0o0.net/ci/github-actions/).
