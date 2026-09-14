# Custom Rulesets

You can create custom rulesets in FunkArr's web UI to handle shows or movies not covered by the community catalog, or to override community rules with your own matching logic.

## Using the RuleSet Builder

1. Open the FunkArr web UI
2. Navigate to **Rulesets**
3. Click **Create Ruleset**
4. Define topic, title patterns, and season/episode extraction
5. Use the **Debugger** to test your rules against live Mediathek data

## Overrides

Local rulesets take priority over community rulesets for the same show. If a community ruleset doesn't match correctly, you can create a local override without waiting for an upstream fix.

## Schema

Rulesets follow a JSON schema. See [`ruleset.schema.json`](https://github.com/st0o0/funkarr/blob/main/data/community/ruleset.schema.json) for the full specification.
