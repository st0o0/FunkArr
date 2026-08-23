using System.Diagnostics.Metrics;

namespace FunkArr.Diagnostics;

public sealed class FunkArrMetrics
{
    public static readonly FunkArrMetrics Instance = new();
    public Meter Meter { get; } = new("FunkArr");
    private FunkArrMetrics() { }
}
