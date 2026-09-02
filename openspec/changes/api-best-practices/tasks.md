## 1. JSON serialization config

- [x] 1.1 Add `ConfigureHttpJsonOptions` in `ServiceSetupContainer` with `CamelCase` naming policy and `JsonStringEnumConverter`

## 2. OpenAPI + Scalar

- [x] 2.1 Add `Scalar.AspNetCore` to `Directory.Packages.props` and `FunkArr.csproj`
- [x] 2.2 Add `AddOpenApi()` in `ServiceSetupContainer`
- [x] 2.3 Add `MapOpenApi()` and `MapScalarApiReference()` in `ApplicationSetupContainer`

## 3. TypedResults + Problem Details

- [x] 3.1 Switch `RuleSetApiEndpoints` to `TypedResults` with Problem Details for errors

## 4. Verification

- [x] 4.1 Build solution, run `dotnet format`, run all tests
