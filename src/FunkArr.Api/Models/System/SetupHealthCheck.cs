namespace FunkArr.Api.Models.System;

public enum CheckStatus
{
    Unknown = 0,
    Ok = 1,
    Warn = 2,
    Fail = 3
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
    string BaseUrl,
    string IndexerApiPath,
    string DownloadApiPath,
    int DefaultPort);
