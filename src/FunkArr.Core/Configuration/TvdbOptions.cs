namespace FunkArr.Core.Configuration;

public sealed class TvdbOptions
{
    public const string SectionName = "FunkArr:Tvdb";
    public string ApiKey { get; set; } = "";
}
