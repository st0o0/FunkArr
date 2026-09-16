namespace FunkArr.Api.Models;

public enum CheckStatus
{
    Ok,
    Warn,
    Fail,
}

public sealed record SetupHealthCheck(
    Dictionary<string, CheckResult> Checks,
    SetupConnectionInfo SetupConnectionInfo);

public sealed record CheckResult(
    CheckStatus Status,
    string? Message = null,
    string? Value = null,
    string? Masked = null,
    string? Path = null,
    string? Version = null)
{
    public static CheckResult Ok(string? message = null) => new(CheckStatus.Ok, message);
    public static CheckResult Warn(string message) => new(CheckStatus.Warn, message);
    public static CheckResult Fail(string message) => new(CheckStatus.Fail, message);
}

public sealed record SetupConnectionInfo(
    string IndexerApiPath,
    string DownloadApiPath,
    int DefaultPort);
