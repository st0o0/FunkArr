namespace FunkArr.Core;

public sealed class FeedOptions
{
    public const string SectionName = "FunkArr:Feed";

    public bool Enabled { get; set; } = true;
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan InitialLookback { get; set; } = TimeSpan.FromHours(48);
    public int PageSize { get; set; } = 100;
    public int MaxPages { get; set; } = 20;
    public TimeSpan Retention { get; set; } = TimeSpan.FromDays(14);
    public double MinConfidence { get; set; } = 0.7;
    public int MaxConcurrency { get; set; } = 4;
}
