## 1. Endpoint Exception Filter

- [x] 1.1 Create `EndpointExceptionFilter` IEndpointFilter in FunkArr.Api
- [x] 1.2 Register filter on all API route groups in RuleSetApiEndpoints, DownloadsApiEndpoints, MediathekApiEndpoints, SystemApiEndpoints
- [x] 1.3 Remove per-endpoint ILoggerFactory injection and try/catch boilerplate from all endpoint files
- [x] 1.4 Register filter on ArrApi route groups (Newznab, Sabnzbd) if they have the same pattern
- [x] 1.5 Verify build compiles, run `dotnet format`
