## 1. Dependencies

- [x] 1.1 Add `Serilog.Formatting.Compact` to `Directory.Packages.props` and `FunkArr.csproj`

## 2. Configuration cleanup

- [x] 2.1 Add `LogFormat` property to `FunkArrOptions` (string, default `"text"`)
- [x] 2.2 Remove `HttpPort` property from `FunkArrOptions`
- [x] 2.3 Add `LogFormat` validation to `FunkArrOptionsValidator` (must be `json` or `text`)
- [x] 2.4 Remove `HttpPort` validation from `FunkArrOptionsValidator` (none existed)
- [x] 2.5 Update `appsettings.json` — remove `HttpPort`, add `LogFormat: "text"`
- [x] 2.6 Update `appsettings.Development.json` — no `HttpPort` present, no change needed

## 3. Logging setup

- [x] 3.1 Update Serilog configuration in `Program.cs` — read `LogFormat` from config, use `CompactJsonFormatter` when `json`, keep current template when `text`
- [x] 3.2 Add `ApplicationVersion` enrichment from entry assembly version
- [x] 3.3 Remove `HttpPort` Kestrel configuration from `Program.cs`, replace with hardcoded port 6969 fallback

## 4. Infrastructure files

- [x] 4.1 Update `Dockerfile` — change `EXPOSE` to `6969`
- [x] 4.2 Update `docker-compose.example.yml` — set port mapping to `8080:6969`, add `FunkArr__LogFormat=json`, remove `FunkArr__HttpPort`

## 5. Verification

- [x] 5.1 Build solution and verify no compilation errors
- [x] 5.2 Run tests and verify all pass (61/61 passed)
