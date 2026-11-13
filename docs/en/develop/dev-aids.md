# Development aids

Tools in the repository that make working on FunkArr easier.

## In the running app

- **Ruleset debugger.** *Rulesets → + New* in the web UI runs your rules against live Mediathek data and shows which entries match and why. It uses `TestScoringWorker`, so it scores exactly like a real search.
- **Scoring history.** Every scoring decision is recorded; the history of a ruleset (*Rulesets → ruleset → History*) shows which rule matched or rejected an entry.
- **Live logs.** *Settings* streams the server log, so you don't need `docker logs` for a quick look.
- **Metrics.** `/metrics` exposes the Prometheus metrics of every domain, see [Observability](/en/observability).

## Dev builds

Add the `dev-build` label to a pull request to publish a container image of it to `ghcr.io/st0o0/funkarr`. Useful for testing a branch on a real server without a release.

## Architecture model

`pnpm --dir docs exec likec4 serve architecture` opens the model in the LikeC4 viewer with every view. The [VS Code extension](https://marketplace.visualstudio.com/items?itemName=likec4.likec4-vscode) adds completion and a live preview. See [Architecture](./architecture#the-architecture-model).

## AI agents

The repository is set up for coding agents, but you don't need one to contribute.

- `AGENTS.md` holds the compact rules, `CLAUDE.md` adds the OpenSpec workflow and which skill to load for which task.
- `.claude/skills/` has templates for recurring work: `funkarr-actor`, `funkarr-message`, `funkarr-endpoint`, `funkarr-test`, the OpenSpec skills, and `e2e-verify`, which runs the manual E2E plan against the dev stack.
- Agents follow the same OpenSpec flow as people, and their pull requests get the same review.
