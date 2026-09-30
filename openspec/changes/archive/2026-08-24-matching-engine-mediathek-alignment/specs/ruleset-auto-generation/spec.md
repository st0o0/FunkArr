## MODIFIED Requirements

### Requirement: Field selection based on topic prefix presence

When generating TitleRules for `ItemTitleExact` or `ItemTitleIncludes` strategies, the RuleSetGeneratorActor SHALL use `field: "topicTitle"` when the analyzed samples show low `TopicPrefixCount` (topic string not present in most item titles). When `TopicPrefixCount` is high (topic string appears in most titles), the generator SHALL continue using `field: "title"`.

#### Scenario: Topic not in titles — use topicTitle
- **WHEN** sample items have `topic = "Tatort"` and titles like `"Die goldene Zeit"`, `"Das Herz der Schlange"` (topic not in title)
- **AND** `TopicPrefixCount` is below 30% of total samples
- **THEN** generated TitleRules SHALL use `field: "topicTitle"` so regex patterns can anchor on the topic prefix

#### Scenario: Topic in titles — use title
- **WHEN** sample items have `topic = "Bibi Blocksberg"` and titles like `"Bibi Blocksberg: Das Dino-Ei"` (topic in title)
- **AND** `TopicPrefixCount` is above 30% of total samples
- **THEN** generated TitleRules SHALL use `field: "title"` as the topic prefix is already present
