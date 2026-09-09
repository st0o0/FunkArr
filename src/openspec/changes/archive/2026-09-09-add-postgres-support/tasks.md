## 1. NuGet Dependencies

- [x] 1.1 Add `Npgsql` version 10.0.3 to `Directory.Packages.props`
- [x] 1.2 Add `<PackageReference Include="Npgsql" />` to `FunkArr.Core/FunkArr.Core.csproj`

## 2. Configuration

- [x] 2.1 Create `FunkArr.Core/PostgresOptions.cs` - sealed class bound to `FunkArr:Postgres` with Host, Port (default 5432), User, Password, Database (default "funkarr"), and `ToConnectionString()` method
- [x] 2.2 Register `PostgresOptions` binding in `ServiceSetupContainer.cs`

## 3. Persistence Provider Switching

- [x] 3.1 Modify `AkkaSetupContainer.cs` to resolve `PostgresOptions`, select provider name and connection string based on `Host` presence

## 4. Docker Compose

- [x] 4.1 Create `docker-compose.postgres.yml` overlay with PostgreSQL 17 service, healthcheck, and FunkArr Postgres env vars
- [x] 4.2 Verify `docker-compose.example.yml` Postgres env var names match `PostgresOptions` properties

## 5. Verification

- [x] 5.1 Run `dotnet build FunkArr.slnx` - verify clean build
- [x] 5.2 Run `dotnet format --verify-no-changes` - verify formatting
- [x] 5.3 Start dev stack with Postgres overlay and confirm FunkArr connects successfully
