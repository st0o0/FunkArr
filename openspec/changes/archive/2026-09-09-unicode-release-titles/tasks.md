## 1. Remove Umlaut normalization

- [x] 1.1 Delete `NormalizeUmlauts()` method from `ReleaseTitleBuilder.cs` and remove its call in `Sanitize()`
- [x] 1.2 Verify `_invalidChars` strip list does not include Umlaut characters (it should not, but confirm)

## 2. Update tests

- [x] 2.1 Update `ReleaseTitleBuilderTests.cs` - change all assertions that expect ASCII-digraph Umlauts to expect Unicode Umlauts instead
- [x] 2.2 Run `dotnet run --project FunkArr.Search.Tests/FunkArr.Search.Tests.csproj` and verify all tests pass

## 3. Verify

- [x] 3.1 Run `dotnet build FunkArr.slnx` to confirm no compile errors
- [x] 3.2 Run `dotnet format --verify-no-changes` to confirm style compliance
