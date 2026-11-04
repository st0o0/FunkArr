using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models.RuleSets;
using FunkArr.Api.Models.Shared;
using FunkArr.Core;
using FunkArr.Messages.RuleSet;

namespace FunkArr.IntegrationTests.Api;

[Collection("RuleSets")]
public sealed class RuleSetMutationTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    private static CreateRuleSetRequest ValidCreateRequest() =>
        new("test-ruleset", "Test Topic",
            new MediaInput("Test Show", MediaType.Show, TvdbId: 99999),
            [new RuleInput("rule-1", Strategy: IdentificationStrategy.SeasonAndEpisodeNumber,
                SeasonRegex: @"S(\d+)", EpisodeRegex: @"E(\d+)")]);

    private static UpdateRuleSetRequest ValidUpdateRequest() =>
        new("Updated Topic",
            new MediaInput("Updated Show", MediaType.Show, TvdbId: 99999),
            [new RuleInput("rule-1", Strategy: IdentificationStrategy.SeasonAndEpisodeNumber,
                SeasonRegex: @"S(\d+)", EpisodeRegex: @"E(\d+)")]);

    [Fact]
    public async Task Create_Success_Returns201()
    {
        var request = ValidCreateRequest();
        var task = _fixture.Client.PostAsJsonAsync("/api/rulesets", request, cancellationToken: TestContext.Current.CancellationToken);

        var probe = _fixture.GetProbe<IRuleSetRegion>();
        probe.ExpectMsg<CreateLocalRuleSet>(cancellationToken: TestContext.Current.CancellationToken);
        probe.Reply(new CreateLocalRuleSetCompleted("test-ruleset"));

        var response = await task;

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CreatedRuleSetResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal("test-ruleset", result.RuleSetId);
    }

    [Fact]
    public async Task Create_ValidationFailed_Returns422()
    {
        var request = ValidCreateRequest();
        var task = _fixture.Client.PostAsJsonAsync("/api/rulesets", request, cancellationToken: TestContext.Current.CancellationToken);

        var probe = _fixture.GetProbe<IRuleSetRegion>();
        probe.ExpectMsg<CreateLocalRuleSet>(cancellationToken: TestContext.Current.CancellationToken);
        probe.Reply(new CreateLocalRuleSetValidationFailed([new RuleSetValidationError("topic", "Topic is required"), new RuleSetValidationError("rules", "At least one rule needed")]));

        var response = await task;

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task Update_Success_Returns200()
    {
        var request = ValidUpdateRequest();
        var task = _fixture.Client.PutAsJsonAsync("/api/rulesets/test-ruleset", request, cancellationToken: TestContext.Current.CancellationToken);

        var probe = _fixture.GetProbe<IRuleSetRegion>();
        probe.ExpectMsg<UpdateLocalRuleSet>(cancellationToken: TestContext.Current.CancellationToken);
        probe.Reply(new UpdateLocalRuleSetCompleted());

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Success_Returns200()
    {
        var task = _fixture.Client.DeleteAsync("/api/rulesets/test-ruleset", TestContext.Current.CancellationToken);

        var probe = _fixture.GetProbe<IRuleSetRegion>();
        probe.ExpectMsg<DeleteLocalRuleSet>(cancellationToken: TestContext.Current.CancellationToken);
        probe.Reply(new DeleteLocalRuleSetCompleted());

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var task = _fixture.Client.DeleteAsync("/api/rulesets/nonexistent", TestContext.Current.CancellationToken);

        var probe = _fixture.GetProbe<IRuleSetRegion>();
        probe.ExpectMsg<DeleteLocalRuleSet>(cancellationToken: TestContext.Current.CancellationToken);
        probe.Reply(new DeleteLocalRuleSetFailed(DeleteLocalRuleSetFailureReason.NotFound));

        var response = await task;

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
