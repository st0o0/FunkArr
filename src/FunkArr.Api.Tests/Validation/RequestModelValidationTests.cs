using System.ComponentModel.DataAnnotations;
using FunkArr.Api.Models.Downloads;
using FunkArr.Api.Models.RuleSets;
using FunkArr.Api.Models.Scoring;
using FunkArr.Api.Models.Shared;

namespace FunkArr.Api.Tests.Validation;

public sealed class RequestModelValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Theory]
    [InlineData("ard-tagesschau")]
    [InlineData("zdf-heute-show")]
    [InlineData("a1")]
    [InlineData("abc")]
    public void CreateRuleSet_valid_kebab_case_ids_pass(string id)
    {
        var request = CreateRuleSetRequest(id);

        var results = Validate(request);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("UPPER", "uppercase letters")]
    [InlineData("has spaces", "spaces")]
    [InlineData("trailing-", "trailing hyphen")]
    [InlineData("-leading", "leading hyphen")]
    [InlineData("double--hyphen", "consecutive hyphens")]
    [InlineData("", "empty string")]
    [InlineData("under_score", "underscore")]
    public void CreateRuleSet_invalid_ids_fail_regex(string id, string _)
    {
        var request = CreateRuleSetRequest(id);

        var results = Validate(request);

        Assert.Contains(results, r => r.MemberNames.Contains("RuleSetId"));
    }

    [Fact]
    public void CreateRuleSet_missing_topic_fails()
    {
        var request = new CreateRuleSetRequest(
            "valid-id", null!, new MediaInput("Show", MediaType.Show), [new RuleInput("r1")]);

        var results = Validate(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Topic"));
    }

    [Fact]
    public void CreateRuleSet_empty_rules_fails()
    {
        var request = new CreateRuleSetRequest(
            "valid-id", "Topic", new MediaInput("Show", MediaType.Show), []);

        var results = Validate(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Rules"));
    }

    [Fact]
    public void DownloadHistory_negative_start_fails()
    {
        var request = new DownloadHistoryRequest(-1, 10, null);

        var results = Validate(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Start"));
    }

    [Fact]
    public void DownloadHistory_zero_limit_fails()
    {
        var request = new DownloadHistoryRequest(0, 0, null);

        var results = Validate(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Limit"));
    }

    [Fact]
    public void DownloadHistory_limit_over_1000_fails()
    {
        var request = new DownloadHistoryRequest(0, 1001, null);

        var results = Validate(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Limit"));
    }

    [Fact]
    public void DownloadHistory_valid_values_pass()
    {
        var request = new DownloadHistoryRequest(0, 50, MediaType.Show);

        var results = Validate(request);

        Assert.Empty(results);
    }

    [Fact]
    public void DownloadHistory_null_values_pass()
    {
        var request = new DownloadHistoryRequest(null, null, null);

        var results = Validate(request);

        Assert.Empty(results);
    }

    [Fact]
    public void TestScoreRequest_empty_rules_fails()
    {
        var request = new TestScoreRequest(0.7f, [],
            [new TestCandidate("title", "topic", "ARD", 3600, 720, null, DateTimeOffset.UtcNow.ToUnixTimeSeconds())]);

        var results = Validate(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Rules"));
    }

    [Fact]
    public void TestScoreRequest_empty_candidates_fails()
    {
        var request = new TestScoreRequest(0.7f,
            [new RuleInput("r1")], []);

        var results = Validate(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Candidates"));
    }

    private static CreateRuleSetRequest CreateRuleSetRequest(string id) =>
        new(id, "Topic", new MediaInput("Show Name", MediaType.Show), [new RuleInput("r1")]);
}
