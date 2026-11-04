# Tasks

## 1. Create Infrastructure subdirectory and move files

- [x] Move `ApiResults.cs` → `Infrastructure/ApiResults.cs`
- [x] Move `ArrSetupClient.cs` → `Infrastructure/ArrSetupClient.cs`
- [x] Move `EndpointExceptionFilter.cs` → `Infrastructure/EndpointExceptionFilter.cs`
- [x] Move `RingBufferSink.cs` → `Infrastructure/RingBufferSink.cs`
- [x] Update namespace declarations if needed (keep `FunkArr.Api` namespace — subfolder is physical organization only)

## 2. Rename SetupArrEndpoints

- [x] Rename `SetupArrEndpoints.cs` → `SetupApiEndpoints.cs`
- [x] Rename class `SetupArrEndpoints` → `SetupApiEndpoints`
- [x] Update `MapSetupArrApi` → `MapSetupApi` extension method name
- [x] Update call site in setup container

## 3. Verify

- [x] `dotnet build src/FunkArr.slnx`
- [x] `dotnet format src/FunkArr.slnx --verify-no-changes`
