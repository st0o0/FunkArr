## Context

Documentation-only change. No architecture or code decisions needed.

## Goals / Non-Goals

**Goals:**
- README that gets an *arr user from zero to working setup
- Accurate feature list and build instructions reflecting current project state

**Non-Goals:**
- Developer/contributor documentation
- Screenshots or visual assets
- Detailed API reference (docker-compose.example.yml covers config)

## Decisions

- **Structure order**: Quick Start first, then setup guides, then deeper topics. Users want to get running, not read feature lists.
- **Configuration**: Reference docker-compose.example.yml rather than duplicating env var docs. It's well-commented and stays in sync with the code.
- **Rulesets section**: Explain the concept (messy Mediathek titles mapped to season/episode) and mention the UI builder. This is FunkArr's differentiator.
- **No em-dashes anywhere**: Use regular dashes per project convention.

## Risks / Trade-offs

- [Setup instructions may drift from actual API behavior] - Mitigated by keeping instructions minimal and linking to the example compose file for config details.
