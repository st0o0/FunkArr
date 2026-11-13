# Contributing

Thanks for wanting to help. This section explains how FunkArr is built, run, tested and released. It does not repeat the code conventions: those live on [anvil](https://anvil.st0o0.net/), with FunkArr as the reference implementation.

## Ways to contribute

| You want to… | Start here | Needs C#? |
|---|---|---|
| Report a bug | [Bug report form](https://github.com/st0o0/funkarr/issues/new?template=bug_report.yml) | No |
| Request a feature | [Feature request form](https://github.com/st0o0/funkarr/issues/new?template=feature_request.yml) | No |
| Ask for a show or movie | [Ruleset request form](https://github.com/st0o0/funkarr/issues/new?template=ruleset_request.yml) | No |
| Write or fix a ruleset | [Rulesets](./rulesets) | No |
| Improve the docs | Edit the page (link at the bottom of every page) | No |
| Fix a bug or build a feature | [Local development](./local-development), then [Architecture](./architecture) | Yes |

## Where the rules are

- **[anvil.st0o0.net](https://anvil.st0o0.net/)** explains the conventions with reasons and examples: [C#](https://anvil.st0o0.net/csharp/coding-standards/), [actors](https://anvil.st0o0.net/akka/actor-design/), [persistence](https://anvil.st0o0.net/akka/persistence/), [APIs](https://anvil.st0o0.net/api/minimal-api/), [testing](https://anvil.st0o0.net/testing/assertions/), [metrics](https://anvil.st0o0.net/observability/metrics/).
- **`AGENTS.md`** in the repository root is the compact version of the same rules that AI agents load. If you change a convention, update it there in the same pull request.

## Planning a change

Non-trivial changes are planned with [OpenSpec](https://github.com/Fission-AI/OpenSpec) before code is written. Current behavior is specified in `openspec/specs/`, proposals live in `openspec/changes/<name>/` (`proposal.md`, `design.md`, `specs/`, `tasks.md`).

1. **Explore**: think the problem through, read the code, compare options.
2. **Propose**: write the change, check it with `openspec validate <name> --strict`.
3. **Apply**: implement the tasks and tick them off.
4. **Archive**: merge the delta specs into `openspec/specs/` and move the change to `openspec/changes/archive/`.

Typos, dependency bumps and obvious one-line fixes don't need a proposal.

## Commits

[Conventional Commits](https://www.conventionalcommits.org/), checked by commitlint in CI. The type decides where the commit shows up in the changelog:

| Type | Changelog section |
|---|---|
| `feat` | Features (for rulesets: New Rulesets) |
| `fix` | Bug Fixes (for rulesets: Ruleset Fixes) |
| `perf`, `refactor`, `docs`, `deps` | Performance, Refactoring, Documentation, Dependencies |
| `test`, `ci`, `build`, `chore`, `style` | hidden |

Scopes are optional; use the domain or area, e.g. `fix(arrapi): …`.

## Before you open a pull request

```bash
dotnet build src/FunkArr.slnx
dotnet format src/FunkArr.slnx --verify-no-changes
dotnet run --project src/FunkArr.<Domain>.Tests/FunkArr.<Domain>.Tests.csproj
```

The build must have zero warnings, and behavior changes come with tests. The pull request template has the full checklist; [Building and testing](./building) lists what CI runs.

Code, specs, commits, issues and pull requests are in English. The docs exist in German and English, so docs changes go into both.

By contributing you agree to the [Code of Conduct](https://github.com/st0o0/funkarr/blob/main/CODE_OF_CONDUCT.md). Report security issues through [SECURITY.md](https://github.com/st0o0/funkarr/blob/main/SECURITY.md), not public issues.
