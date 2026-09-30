## REMOVED Requirements

### Requirement: Kestrel HTTP configuration
**Reason:** Port is now controlled via `ASPNETCORE_URLS` environment variable set in the Dockerfile. No application-level port configuration needed.
**Migration:** Remove the Kestrel configuration block from Program.cs. Set `ENV ASPNETCORE_URLS=http://+:6969` in Dockerfiles. For local development, set `"Urls": "http://localhost:5000"` in appsettings.Development.json.
