namespace FunkArr.Api.Models;

public sealed record SetupHealthCheck(
    Dictionary<string, CheckResult> Checks,
    SetupConnectionInfo SetupConnectionInfo);

public sealed record CheckResult(
    string Status,
    string? Message = null,
    string? Value = null,
    string? Masked = null,
    string? Path = null,
    string? Version = null)
{
    public static CheckResult Ok(string? message = null) => new("ok", message);
    public static CheckResult Warn(string message) => new("warn", message);
    public static CheckResult Fail(string message) => new("fail", message);
}

public sealed record SetupConnectionInfo(
    string IndexerApiPath,
    string DownloadApiPath,
    int DefaultPort);
