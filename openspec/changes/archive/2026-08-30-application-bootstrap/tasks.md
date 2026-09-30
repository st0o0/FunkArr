## 1. Configuration

- [x] 1.1 Create `FunkArrOptions` class in `src/FunkArr/Configuration/` with `ApiKey`, `PersistencePath`, and `SectionName` constant
- [x] 1.2 Create `appsettings.json` with `FunkArr` section defaults, Serilog minimum levels, and Kestrel URL
- [x] 1.3 Create `appsettings.Development.json` with dev-appropriate Serilog overrides

## 2. Servus Setup Containers

- [x] 2.1 Create `FunkArrServiceSetup : IServiceSetupContainer` — register FunkArrOptions binding with ValidateOnStart, add health checks
- [x] 2.2 Create `FunkArrActorSystemSetup : ActorSystemSetupContainer` — system name "funkarr", clear loggers, add LoggerFactory
- [x] 2.3 Create `FunkArrApplicationSetup : ApplicationSetupContainer<WebApplication>` — map `/healthz` and `/alive` endpoints

## 3. Host Wiring

- [x] 3.1 Rewrite `Program.cs` — Serilog setup, Kestrel config, AppBuilder chain with 3 setup containers, `await runner.RunAsync()`
- [x] 3.2 Build and verify: `dotnet build FunkArr.slnx` succeeds, `dotnet run` boots with Serilog output, `/alive` returns 200
