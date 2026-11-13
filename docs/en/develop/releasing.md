# Releasing

Releases are automated with [release-please](https://github.com/googleapis/release-please). Nobody tags by hand.

```
merge to main ─▶ release PR updated ─▶ release PR merged ─▶ tag + GitHub release
                                                              ├─▶ image ghcr.io/st0o0/funkarr
                                                              └─▶ docs deploy
```

1. Every merge to `main` updates a release pull request with the next version and the changelog. The commit types decide both, see [Contributing](./#commits).
2. Merging the release pull request tags the release, publishes the container image and deploys this site.

The jobs come from the shared workflows in [st0o0/github-workflows](https://github.com/st0o0/github-workflows) (`release-please.yml`, `docker-release.yml`, `docs.yml`).

## Two components

`release-please-config.json` defines two independent components:

| Component | Path | Tag |
|---|---|---|
| `funkarr` | repository root | `funkarr-vX.Y.Z` |
| `rulesets` | `data/community/` | `rulesets-vX.Y.Z` |

A new ruleset therefore ships without a FunkArr release, see [Rulesets](./rulesets).

## Versioning

FunkArr is at 0.x. A `feat` bumps the patch version and a breaking change bumps the minor version. Breaking changes need no migration code or compatibility shims, with one exception: persisted data. Persisted enum values and records stay readable, because users' journals must survive an update.
