## 1. Extract error from stderr

- [x] 1.1 In `FfmpegRunner.cs`, replace `CapStderr()` with `ExtractError()` that parses stderr for known error patterns and returns the meaningful line
- [x] 1.2 Handle patterns: "HTTP error", "Error opening input", "Server returned", "Invalid data found"
- [x] 1.3 Fallback: return last non-empty line if no pattern matches

## 2. Tests

- [x] 2.1 Add unit tests in `FunkArr.Download.Tests` for `ExtractError` with each error pattern scenario
- [x] 2.2 Test the fallback case (unknown error)
- [x] 2.3 Test null/empty stderr

## 3. Verify

- [x] 3.1 Run `dotnet build FunkArr.slnx`
- [x] 3.2 Run `dotnet run --project FunkArr.Download.Tests/FunkArr.Download.Tests.csproj`
- [x] 3.3 Run `dotnet format --verify-no-changes`
