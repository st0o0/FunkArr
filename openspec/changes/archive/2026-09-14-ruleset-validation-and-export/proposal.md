## Why

Local rulesets have no structural validation — the API writes any JSON body directly to disk. Invalid rulesets silently break at runtime (the merger ignores what it doesn't understand). Users get no feedback when they create a broken ruleset. Additionally, there is no way to export a local ruleset as a standalone community-ready file — merged rulesets (local overrides on community) can't be shared without manually reconstructing the full JSON.

## What Changes

- **Schema validation on save**: Validate every local ruleset against `ruleset.schema.json` on POST and PUT. Embed the schema as an assembly resource so it's always available. Return all validation errors at once with human-readable messages that reference rules by ID/index and explain what to fix.
- **Business rule validation**: Check values that the JSON schema can't enforce — confidence range (0–1), strategy enum membership, regex pattern syntax.
- **Community export endpoint**: New `GET /api/rulesets/{id}/export` that produces a flattened, standalone, schema-validated JSON file. For merged rulesets, resolves the merge (community + local) into a single self-contained ruleset with override-specific fields (`standalone`, `disable`) stripped.
- **Export UI**: "Export for Community" button on the ruleset detail page, visible when the ruleset has a local component.
- **Validation error display**: Show validation errors in the ruleset builder UI so users can fix issues before saving.

## Capabilities

### New Capabilities
- `ruleset-schema-validation`: Runtime validation of ruleset JSON against the embedded schema with human-readable error mapping
- `ruleset-community-export`: Flatten and export a ruleset as standalone community-ready JSON

### Modified Capabilities
- `ruleset-api`: Add validation on POST/PUT and new export endpoint
- `ruleset-builder-ui`: Display validation errors, add export button on detail page

## Impact

- **FunkArr.RuleSet**: New `IRuleSetValidator` service, embedded schema resource, export flattening logic
- **FunkArr.Api**: Validation in create/update handlers, new export endpoint, structured error responses
- **FunkArr.UI**: Validation error display in builder, export button on detail page
- **FunkArr.Core**: DI registration for validator
- **Dependencies**: Needs a JSON Schema validation library (e.g. `JsonSchema.Net` or `NJsonSchema`)
