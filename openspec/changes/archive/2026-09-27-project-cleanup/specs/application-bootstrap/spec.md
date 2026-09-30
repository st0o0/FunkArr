## MODIFIED Requirements

### Requirement: Servus AppBuilder startup
The host SHALL use `AppBuilder.Create(builder, b => b.Build())` with setup containers chained via `.WithSetup<T>()` in this order:
1. `LoggingSetupContainer`
2. `TelemetrySetupContainer`
3. `CoreSetupContainer`
4. `AkkaSetupContainer`
5. `SearchSetupContainer`
6. `DownloadSetupContainer`
7. `ScoringSetupContainer`
8. `RuleSetSetupContainer`
9. `EnrichmentSetupContainer`
10. `ArrApiSetupContainer`
11. `ApplicationSetupContainer`

#### Scenario: Host boots with updated container chain
- **WHEN** `dotnet run` is executed from `src/FunkArr/`
- **THEN** the application starts without errors using the updated container chain

#### Scenario: Setup containers are invoked in order
- **WHEN** the host boots
- **THEN** containers execute in the order listed above, with infrastructure before domains and ApplicationSetupContainer last
