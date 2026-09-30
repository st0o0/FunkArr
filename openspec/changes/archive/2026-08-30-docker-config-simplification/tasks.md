## 1. Remove Kestrel config from Program.cs

- [x] 1.1 Remove the `ASPNETCORE_URLS` check and `ConfigureKestrel` block from Program.cs

## 2. Dockerfile updates

- [x] 2.1 Add `ENV ASPNETCORE_URLS=http://+:6969` to both `Dockerfile` and `Dockerfile.dev`

## 3. Local development

- [x] 3.1 Add `"Urls": "http://localhost:5000"` to `appsettings.Development.json`

## 4. Verify

- [x] 4.1 Build, format check, and verify: `dotnet build` succeeds, `dotnet run` boots on port 5000 (Development), `/alive` returns 200
