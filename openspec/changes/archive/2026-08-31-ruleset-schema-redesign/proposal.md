## Why

The RuleSet JSON schema and C# model diverged during the match-intelligence change. The schema defines features the C# model doesn't have (`overrides`, `$type` discriminator), the C# model uses loose strings where the schema has enums, and the override system is fundamentally broken — it uses index-based rule removal that breaks when the community ruleset changes order. The `generated` source layer no longer exists, leaving dead concepts in both schema and model. Additionally, `source` is redundant with the file's directory location.

## What Changes

- **BREAKING**: Remove `source` field from ruleset files — runtime derives source from file path (`data/community/` vs `data/local/`)
- **BREAKING**: Remove `overrides` config — replaced by extend-by-default merge system
- **BREAKING**: Add required `id` field to all rules (kebab-case, `^[a-z][a-z0-9-]{2,}$`)
- **BREAKING**: Remove `$type` discriminator from filter nodes — structural detection (has `all`/`any`/`not` → group, has `field`/`op`/`value` → leaf)
- **BREAKING**: Remove `exactMatch` from FilterOp enum — duplicate of `eq`
- Add `standalone` boolean to local rulesets (default false — extend is default when community base exists)
- Add `disable` string array to local rulesets (rule IDs to skip from community base)
- Change `MediaRef.Type` from loose string to `MediaType` enum (`show`, `movie`)
- Build `RuleSetResolver` — merge algorithm: same-ID replaces, new-ID adds, `disable` skips, aliases union, confidence/media local-wins
- Update community ruleset schema file to match

## Capabilities

### New Capabilities
- `ruleset-layering`: Extend-by-default override system where local rulesets auto-extend community (same topic), with `standalone: true` opt-out, `disable` for skipping base rules, and same-ID replacement. Includes merge algorithm for aliases (union), confidence (local-wins), media (local-wins).

### Modified Capabilities
- `matchmagic-data-model`: Remove `Source` from RuleSet, add `Standalone`/`Disable` fields, add `Id` to Rule, change `MediaRef.Type` to `MediaType` enum, remove `exactMatch` from FilterOp.

## Impact

- `data/community/ruleset.schema.json` — full rewrite
- `src/FunkArr.MatchMagic/` — RuleSet.cs, Rule.cs, MediaRef.cs, new MediaType.cs, new RuleSetResolver.cs
- `src/FunkArr.MatchMagic.Tests/` — update deserialization and evaluation tests, update test resource JSON files
- All existing community ruleset JSON files need `id` fields added to rules and `source` removed
- `.github/workflows/validate-rulesets.yml` — schema validation may need update
