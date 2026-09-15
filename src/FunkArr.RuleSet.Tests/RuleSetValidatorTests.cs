namespace FunkArr.RuleSet.Tests;

public sealed class RuleSetValidatorTests
{
    private readonly RuleSetValidator _validator = new();

    private const string ValidRuleSetJson = """
        {
          "topic": "Test Show",
          "aliases": [],
          "media": {
            "name": "Test Show",
            "type": "show",
            "imdbId": "tt1234567"
          },
          "confidence": 0.9,
          "rules": [
            {
              "id": "airdate",
              "priority": 0,
              "strategy": "itemTitleEqualsAirdate",
              "filters": {
                "all": [
                  { "field": "duration", "op": "greaterThan", "value": "30" }
                ]
              }
            }
          ]
        }
        """;

    [Fact]
    public void Validate_valid_ruleset_returns_no_errors()
    {
        var errors = _validator.Validate(ValidRuleSetJson);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_missing_topic_returns_error()
    {
        var json = """
            {
              "media": { "name": "Test", "type": "show" },
              "rules": []
            }
            """;

        var errors = _validator.Validate(json);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("topic", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_missing_media_returns_error()
    {
        var json = """
            {
              "topic": "Test Show",
              "rules": []
            }
            """;

        var errors = _validator.Validate(json);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("media", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_confidence_as_string_returns_error()
    {
        var json = """
            {
              "topic": "Test Show",
              "media": { "name": "Test", "type": "show" },
              "confidence": "high",
              "rules": []
            }
            """;

        var errors = _validator.Validate(json);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Field.Contains("confidence"));
    }

    [Fact]
    public void Validate_invalid_strategy_returns_error()
    {
        var json = """
            {
              "topic": "Test Show",
              "media": { "name": "Test", "type": "show" },
              "rules": [
                {
                  "id": "bad-rule",
                  "priority": 0,
                  "strategy": "nonExistentStrategy"
                }
              ]
            }
            """;

        var errors = _validator.Validate(json);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Field.Contains("rules[bad-rule]"));
    }

    [Fact]
    public void Validate_invalid_regex_in_title_rules_returns_error()
    {
        var json = """
            {
              "topic": "Test Show",
              "media": { "name": "Test", "type": "show" },
              "rules": [
                {
                  "id": "title-rule",
                  "priority": 0,
                  "strategy": "itemTitleExact",
                  "titleRules": [
                    { "type": "regex", "field": "title", "pattern": "(unclosed" }
                  ]
                }
              ]
            }
            """;

        var errors = _validator.Validate(json);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Field.Contains("titleRules") && e.Message.Contains("regex", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_invalid_regex_in_season_regex_returns_error()
    {
        var json = """
            {
              "topic": "Test Show",
              "media": { "name": "Test", "type": "show" },
              "rules": [
                {
                  "id": "season-rule",
                  "priority": 0,
                  "strategy": "seasonAndEpisodeNumber",
                  "seasonRegex": "[invalid",
                  "episodeRegex": "E(\\d+)"
                }
              ]
            }
            """;

        var errors = _validator.Validate(json);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Field.Contains("seasonRegex") && e.Message.Contains("regex", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_multiple_errors_returned_at_once()
    {
        var json = """
            {
              "confidence": "not-a-number",
              "rules": [
                {
                  "id": "bad-rule",
                  "strategy": "invalidStrategy"
                }
              ]
            }
            """;

        var errors = _validator.Validate(json);

        Assert.True(errors.Count >= 2);
    }

    [Fact]
    public void Validate_invalid_json_returns_error()
    {
        var errors = _validator.Validate("not json at all");

        Assert.Single(errors);
        Assert.Equal("(root)", errors[0].Field);
        Assert.Contains("Invalid JSON", errors[0].Message);
    }

    [Fact]
    public void Validate_json_array_instead_of_object_returns_error()
    {
        var errors = _validator.Validate("[]");

        Assert.NotEmpty(errors);
    }
}
