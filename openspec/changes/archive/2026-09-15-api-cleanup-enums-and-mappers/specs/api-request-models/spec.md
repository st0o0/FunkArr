## MODIFIED Requirements

### Requirement: TestRule uses IdentificationStrategy enum
The `TestRule` record SHALL use `IdentificationStrategy?` for the `Strategy` property instead of `string?`. The `[JsonPropertyName]` attributes on the enum members handle JSON deserialization automatically.

#### Scenario: Deserialize test rule with strategy
- **WHEN** the API receives a test request with `"strategy": "itemTitleIncludes"`
- **THEN** `TestRule.Strategy` is `IdentificationStrategy.TitleIncludes`

### Requirement: TestFilterNode uses typed enums
The `TestFilterNode` record SHALL use `FilterField?` for `Field` and `FilterOp?` for `Op` instead of strings.

#### Scenario: Deserialize test filter with typed enums
- **WHEN** the API receives a filter with `"field": "duration"` and `"op": "greaterThan"`
- **THEN** `TestFilterNode.Field` is `FilterField.Duration` and `TestFilterNode.Op` is `FilterOp.GreaterThan`

### Requirement: TestTitleRule uses TitlePartType enum
The `TestTitleRule` record SHALL use `TitlePartType` for the `Type` property instead of string.

#### Scenario: Deserialize title rule type
- **WHEN** the API receives a title rule with `"type": "regex"`
- **THEN** `TestTitleRule.Type` is `TitlePartType.Regex`
