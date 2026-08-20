using System.Text.Json;

namespace FunkArr.RuleSet;

public static class RuleSetFileWriter
{
    public static void Write(string directory, RuleSetFile ruleSet)
    {
        Directory.CreateDirectory(directory);

        var slug = TopicSlugGenerator.Generate(ruleSet.Topic);
        var path = Path.Combine(directory, $"{slug}.json");
        var json = JsonSerializer.Serialize(ruleSet, RuleSetJsonOptions.Default);
        File.WriteAllText(path, json);
    }
}
