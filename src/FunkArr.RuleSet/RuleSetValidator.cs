using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using FunkArr.Core;
using Json.Schema;

namespace FunkArr.RuleSet;

public sealed class RuleSetValidator : IRuleSetValidator
{
    private static readonly JsonSchema _schema = LoadSchema();

    private static JsonSchema LoadSchema()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("FunkArr.RuleSet.ruleset.schema.json")
                           ?? throw new InvalidOperationException("Embedded ruleset schema not found");
        using var reader = new StreamReader(stream);
        return JsonSchema.FromText(reader.ReadToEnd());
    }

    public IReadOnlyList<RuleSetValidationError> Validate(string json)
    {
        var errors = new List<RuleSetValidationError>();

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            errors.Add(new RuleSetValidationError("(root)", $"Invalid JSON: {ex.Message}"));
            return errors;
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add(new RuleSetValidationError("(root)", "Ruleset must be a JSON object"));
                return errors;
            }

            ValidateSchema(doc.RootElement, errors);
            ValidateRegexPatterns(doc.RootElement, errors);
        }

        return errors;
    }

    private static void ValidateSchema(JsonElement root, List<RuleSetValidationError> errors)
    {
        var options = new EvaluationOptions
        {
            OutputFormat = OutputFormat.List,
        };

        var result = _schema.Evaluate(root, options);
        if (result.IsValid)
        {
            return;
        }

        var ruleIds = ExtractRuleIds(root);

        foreach (var detail in result.Details ?? [])
        {
            if (detail.IsValid || detail.Errors is null || detail.Errors.Count == 0)
            {
                continue;
            }

            var instancePath = detail.InstanceLocation.ToString();
            var friendlyPath = HumanizePath(instancePath, ruleIds);

            foreach (var error in detail.Errors)
            {
                var message = HumanizeError(error.Key, error.Value, friendlyPath);
                errors.Add(new RuleSetValidationError(friendlyPath, message));
            }
        }
    }

    private static void ValidateRegexPatterns(JsonElement root, List<RuleSetValidationError> errors)
    {
        if (!root.TryGetProperty("rules", out var rulesEl) || rulesEl.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var ruleIndex = 0;
        foreach (var rule in rulesEl.EnumerateArray())
        {
            var ruleRef = GetRuleRef(rule, ruleIndex);

            ValidateRegexField(rule, "seasonRegex", $"rules[{ruleRef}].seasonRegex", errors);
            ValidateRegexField(rule, "episodeRegex", $"rules[{ruleRef}].episodeRegex", errors);

            if (rule.TryGetProperty("titleRules", out var titleRulesEl) && titleRulesEl.ValueKind == JsonValueKind.Array)
            {
                var titleIndex = 0;
                foreach (var titleRule in titleRulesEl.EnumerateArray())
                {
                    if (titleRule.TryGetProperty("type", out var typeEl) &&
                        typeEl.GetString() == "regex")
                    {
                        ValidateRegexField(titleRule, "pattern",
                            $"rules[{ruleRef}].titleRules[{titleIndex}].pattern", errors);
                    }

                    titleIndex++;
                }
            }

            ruleIndex++;
        }
    }

    private static void ValidateRegexField(JsonElement element, string propertyName, string path,
        List<RuleSetValidationError> errors)
    {
        if (!element.TryGetProperty(propertyName, out var patternEl) || patternEl.ValueKind != JsonValueKind.String)
        {
            return;
        }

        var pattern = patternEl.GetString();
        if (string.IsNullOrEmpty(pattern))
        {
            return;
        }

        try
        {
            _ = new Regex(pattern);
        }
        catch (RegexParseException ex)
        {
            errors.Add(new RuleSetValidationError(path, $"Invalid regex pattern: {ex.Message}"));
        }
    }

    private static Dictionary<int, string> ExtractRuleIds(JsonElement root)
    {
        var ruleIds = new Dictionary<int, string>();
        if (!root.TryGetProperty("rules", out var rulesEl) || rulesEl.ValueKind != JsonValueKind.Array)
        {
            return ruleIds;
        }

        var index = 0;
        foreach (var rule in rulesEl.EnumerateArray())
        {
            if (rule.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String)
            {
                var id = idEl.GetString();
                if (!string.IsNullOrEmpty(id))
                {
                    ruleIds[index] = id;
                }
            }

            index++;
        }

        return ruleIds;
    }

    private static string GetRuleRef(JsonElement rule, int index)
    {
        if (rule.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String)
        {
            var id = idEl.GetString();
            if (!string.IsNullOrEmpty(id))
            {
                return id;
            }
        }

        return index.ToString();
    }

    private static string HumanizePath(string jsonPointer, Dictionary<int, string> ruleIds)
    {
        if (string.IsNullOrEmpty(jsonPointer) || jsonPointer == "/")
        {
            return "(root)";
        }

        var segments = jsonPointer.TrimStart('/').Split('/');
        var parts = new List<string>();
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (int.TryParse(segment, out var idx) && i > 0)
            {
                var parent = segments[i - 1];
                if (parent == "rules" && ruleIds.TryGetValue(idx, out var ruleId))
                {
                    parts[^1] = $"rules[{ruleId}]";
                }
                else
                {
                    parts[^1] = $"{parent}[{idx}]";
                }
            }
            else
            {
                parts.Add(segment);
            }
        }

        return string.Join('.', parts);
    }

    private static string HumanizeError(string errorKey, string errorValue, string path)
    {
        return errorKey switch
        {
            "required" => $"Required property missing: {errorValue}",
            "type" => $"Wrong type: {errorValue}",
            "enum" => $"Invalid value. {errorValue}",
            "minimum" or "maximum" => $"Value out of range: {errorValue}",
            "minLength" => "Value must not be empty",
            "pattern" => $"Value does not match required pattern: {errorValue}",
            "additionalProperties" => $"Unknown property: {errorValue}",
            "oneOf" => $"Value does not match any allowed format: {errorValue}",
            _ => errorValue,
        };
    }
}
