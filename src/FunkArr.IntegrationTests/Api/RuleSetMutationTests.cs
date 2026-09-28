using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models;
using FunkArr.Core;
using FunkArr.Messages.RuleSet;

namespace FunkArr.IntegrationTests.Api;

[Collection("RuleSets")]
public sealed class RuleSetMutationTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    private static CreateRuleSetRequest ValidCreateRequest() =>
        new("test-ruleset", "Test Topic",
            new MediaInput("Test Show", "show", TvdbId: 99999),
            [new RuleInput("rule-1", Strategy: IdentificationStrategy.SeasonAndEpisodeNumber,
                SeasonRegex: @"S(\d+)", EpisodeRegex: @"E(\d+)")]);

    private static UpdateRuleSetRequest ValidUpdateRequest() =>
        new("Updated Topic",
            new MediaInput("Updated Show", "show", TvdbId: 99999),
            [new RuleInput("rule-1", Strategy: IdentificationStrategy.SeasonAndEpisodeNumber,
                SeasonRegex: @"S(\d+)", EpisodeRegex: @"E(\d+)")]);

    [Fact]
    public async Task Create_Success_Returns201()
    {
        var request = ValidCreateRequest();
        var task = _fixture.Client.PostAsJsonAsync("/api/rulesets", request);

        var probe = _fixture.GetProbe<IRuleSetRegion>();
        probe.ExpectMsg<CreateLocalRuleSet>();
        probe.Reply(new CreateLocalRuleSetCompleted("test-ruleset"));

        var response = await task;

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CreatedRuleSetResponse>();
        Assert.NotNull(result);
        Assert.Equal("test-ruleset", result.RuleSetId);
    }

    [Fact]
    public async Task Create_ValidationFailed_Returns422()
    {
        var request = ValidCreateRequest();
        var task = _fixture.Client.PostAsJsonAsync("/api/rulesets", request);

        var probe = _fixture.GetProbe<IRuleSetRegion>();
        probe.ExpectMsg<CreateLocalRuleSet>();
        probe.Reply(new CreateLocalRuleSetValidationFailed([new RuleSetValidationError("topic", "Topic is required"), new RuleSetValidationError("rules", "At least one rule needed")]));

        var response = await task;

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task Update_Success_Returns200()
    {
        var request = ValidUpdateRequest();
        var task = _fixture.Client.PutAsJsonAsync("/api/rulesets/test-ruleset", request);

        var probe = _fixture.GetProbe<IRuleSetRegion>();
        probe.ExpectMsg<UpdateLocalRuleSet>();
        probe.Reply(new UpdateLocalRuleSetCompleted());

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Success_Returns200()
    {
        var task = _fixture.Client.DeleteAsync("/api/rulesets/test-ruleset");

        var probe = _fixture.GetProbe<IRuleSetRegion>();
        probe.ExpectMsg<DeleteLocalRuleSet>();
        probe.Reply(new DeleteLocalRuleSetCompleted());

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var task = _fixture.Client.DeleteAsync("/api/rulesets/nonexistent");

        var probe = _fixture.GetProbe<IRuleSetRegion>();
        probe.ExpectMsg<DeleteLocalRuleSet>();
        probe.Reply(new DeleteLocalRuleSetFailed(DeleteLocalRuleSetFailureReason.NotFound));

        var response = await task;

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
