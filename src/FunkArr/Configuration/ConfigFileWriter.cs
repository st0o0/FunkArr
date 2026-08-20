using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace FunkArr.Configuration;

public sealed class ConfigFileWriter
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _configPath;
    private readonly Lock _lock = new();

    public ConfigFileWriter(IOptions<FunkArrOptions> options)
    {
        var dataDir = Path.GetDirectoryName(options.Value.PersistencePath) ?? "data";
        _configPath = Path.Combine(dataDir, "config.json");
    }

    public void Write(JsonObject partial)
    {
        lock (_lock)
        {
            var existing = ReadExisting();
            Merge(existing, partial);

            var dir = Path.GetDirectoryName(_configPath);
            if (dir is not null)
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(_configPath, existing.ToJsonString(WriteOptions));
        }
    }

    public JsonObject Read()
    {
        lock (_lock)
        {
            return ReadExisting();
        }
    }

    private JsonObject ReadExisting()
    {
        if (!File.Exists(_configPath))
        {
            return new JsonObject();
        }

        var json = File.ReadAllText(_configPath);
        return JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
    }

    private static void Merge(JsonObject target, JsonObject source)
    {
        foreach (var (key, value) in source)
        {
            if (value is JsonObject sourceObj && target[key] is JsonObject targetObj)
            {
                Merge(targetObj, sourceObj);
            }
            else
            {
                target[key] = value?.DeepClone();
            }
        }
    }
}
