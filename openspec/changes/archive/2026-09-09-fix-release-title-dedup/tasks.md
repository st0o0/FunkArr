## 1. Add StripTopicFromTitle to ReleaseTitleBuilder

- [x] 1.1 Add `StripTopicFromTitle(string topic, string title)` method that removes topic as prefix (with `: ` or ` - ` delimiter) or suffix (with ` - ` or ` – ` delimiter), case-insensitive
- [x] 1.2 Call StripTopicFromTitle in `Build()` before sanitizing the title: `parts.Add(Sanitize(StripTopicFromTitle(topic, title)))`

## 2. Fix ruleset data

- [x] 2.1 `unser-sandmaennchen.json`: remove the media block (tvdbId 289772 is "Jan & Henry", not "Unser Sandmannchen")
- [x] 2.2 `fernsehfilme-und-serien-serien.json`: remove the media block (catch-all ruleset, not a specific series)
- [x] 2.3 `elefant-tiger-und-co.json`: change media.name from "Elefant, Tiger und Co." to "Elefant, Tiger & Co." to match TVDB

## 3. Update tests

- [x] 3.1 Add tests for StripTopicFromTitle: prefix with colon, suffix with dash/en-dash, no-match, empty-after-strip
- [x] 3.2 Update existing test expectations if any titles now strip topic prefix

## 4. Verify

- [x] 4.1 Run `dotnet build FunkArr.slnx`
- [x] 4.2 Run Search tests
- [x] 4.3 Run `dotnet format --verify-no-changes`
