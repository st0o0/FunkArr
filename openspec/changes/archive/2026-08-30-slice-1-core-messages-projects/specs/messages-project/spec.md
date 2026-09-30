## ADDED Requirements

### Requirement: Messages project depends only on Core
The `FunkArr.Messages` project SHALL have a single ProjectReference to `FunkArr.Core` and no other project or package dependencies beyond the BCL.

#### Scenario: Messages project compiles with Core reference only
- **WHEN** `FunkArr.Messages.csproj` is built
- **THEN** it SHALL compile with only a ProjectReference to `FunkArr.Core` and BCL dependencies

### Requirement: Messages project contains standalone message files
The `FunkArr.Messages` project SHALL contain message files that are already separated from actor implementations, including search coordinator messages and search messages.

#### Scenario: Standalone messages resolve from Messages project
- **WHEN** actor code references `SearchRequest.Tv` or `QueryItems`
- **THEN** it SHALL resolve the type from the `FunkArr.Messages` project
