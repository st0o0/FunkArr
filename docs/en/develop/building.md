# Building and testing

All commands run from the repository root.

## Prerequisites

| Tool | Version |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | as pinned in `src/global.json` |
| [Node.js](https://nodejs.org/) | 26, as in CI and the Dockerfile |
| [pnpm](https://pnpm.io/) | as in `packageManager` of each `package.json` |
| [Docker](https://docs.docker.com/get-docker/) with Compose | current |

## Backend

```bash
dotnet build src/FunkArr.slnx
dotnet format src/FunkArr.slnx --verify-no-changes
```

`TreatWarningsAsErrors` is on for every project. Fix the cause of a warning instead of suppressing it. `dotnet format src/FunkArr.slnx` fixes formatting.

Tests use xUnit v3 on Microsoft.Testing.Platform. Each test project is an executable, so run it with `dotnet run`, not `dotnet test`:

```bash
dotnet run --project src/FunkArr.Download.Tests/FunkArr.Download.Tests.csproj
dotnet run --project src/FunkArr.Download.Tests/FunkArr.Download.Tests.csproj -- --filter-method "*DispatchNext*"
```

How tests are written (TestKit, probes, assertions) is on anvil: [actor testing](https://anvil.st0o0.net/testing/actor-testing/), [assertions](https://anvil.st0o0.net/testing/assertions/).

## Web UI

```bash
pnpm --dir src/FunkArr.UI install
pnpm --dir src/FunkArr.UI build        # vue-tsc type check and Vite build
pnpm --dir src/FunkArr.UI test         # Vitest
pnpm --dir src/FunkArr.UI test:ui      # Playwright (once: exec playwright install chromium)
```

## Docs

```bash
pnpm --dir docs install
pnpm --dir docs dev
pnpm --dir docs build                  # also fails on dead links
```

## What CI runs

| Check | Runs | When |
|---|---|---|
| `dotnet` | build, format check, all test projects | every pull request |
| `frontend` | UI install and build | every pull request |
| `commits` | commitlint | every pull request |
| `UI Tests` | type check, Vitest, Playwright | changes in `src/FunkArr.UI/` |
| `validate-rulesets` | JSON schema check | changes in `data/community/` |
| `Architecture model` | `likec4 validate` | changes in `docs/architecture/` |
| `Security` | dependency and image scans | Dockerfile or package changes, weekly |

The build and test jobs come from the shared workflows in [st0o0/github-workflows](https://github.com/st0o0/github-workflows), see [CI on anvil](https://anvil.st0o0.net/ci/github-actions/).
