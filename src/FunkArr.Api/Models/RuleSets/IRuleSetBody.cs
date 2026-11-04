namespace FunkArr.Api.Models;

public interface IRuleSetBody
{
    string Topic { get; }
    string[]? Aliases { get; }
    MediaInput Media { get; }
    float? Confidence { get; }
    RuleInput[] Rules { get; }
    bool? Standalone { get; }
    string[]? Disable { get; }
}
