## 1. Fix episode-only formatting

- [x] 1.1 In `ReleaseTitleBuilder.AppendTvIdentifier()`, change the episode-only branch from `E{padded}` to `S01E{padded}` (add "S01" prefix when season is null but episode is present)

## 2. Update tests

- [x] 2.1 Update `ReleaseTitleBuilderTests` - change the episode-only test assertion from `E312` to `S01E312`
- [x] 2.2 Add a test for high absolute episode numbers (e.g., E1666 -> S01E1666)
- [x] 2.3 Run `dotnet run --project FunkArr.Search.Tests/FunkArr.Search.Tests.csproj`

## 3. Verify

- [x] 3.1 Run `dotnet build FunkArr.slnx`
- [x] 3.2 Run `dotnet format --verify-no-changes`
