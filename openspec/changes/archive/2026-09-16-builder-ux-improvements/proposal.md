## Why

The Builder's filter section shows three groups (all/any/not) each with a header, add button, and "(keine Bedingungen)" text even when empty. Since "any" and "not" are rarely used, this creates visual noise. The Save button is at the bottom requiring scroll on long forms.

## What Changes

- Collapse empty "any" and "not" filter sections — only show header with add button, hide "(keine Bedingungen)" text
- Keep "all" always visible since it's the most common filter group
- Move save/cancel to sticky position for long forms

## Capabilities

### New Capabilities
_None._

### Modified Capabilities
- `ruleset-builder-ui`: Filter section collapse behavior for empty groups

## Impact
- **Frontend only**: `RuleSetBuilder.vue`
