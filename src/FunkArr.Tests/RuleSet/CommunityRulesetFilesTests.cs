using System.Text.Json;
using FunkArr.RuleSet;

namespace FunkArr.Tests.RuleSet;

public class CommunityRulesetFilesTests
{
    private static readonly string RepoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static readonly string RulesetsDir = Path.Combine(RepoRoot, "data", "community", "rulesets");

    public static TheoryData<string> RulesetFiles()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.GetFiles(RulesetsDir, "*.json"))
            data.Add(Path.GetFileName(file));
        return data;
    }

    [Theory]
    [MemberData(nameof(RulesetFiles))]
    public void Deserializes_WithoutErrors(string fileName)
    {
        var json = File.ReadAllText(Path.Combine(RulesetsDir, fileName));

        var result = JsonSerializer.Deserialize<RuleSetFile>(json, RuleSetJsonOptions.Default);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Topic);
        Assert.NotEmpty(result.Rules);
    }

    [Fact]
    public void AllFiles_ExpectedCount()
    {
        var files = Directory.GetFiles(RulesetsDir, "*.json");

        Assert.Equal(59, files.Length);
    }
}
