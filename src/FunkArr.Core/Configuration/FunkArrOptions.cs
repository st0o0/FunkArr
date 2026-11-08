namespace FunkArr.Core.Configuration;

public sealed class FunkArrOptions
{
    public const string SectionName = "FunkArr";

    public string ApiKey { get; set; } = "funkarr-default-api-key";

    public string BaseUrl { get; set; } = "";

    public string DataPath { get; set; } = "data";
}
