using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FunkArr.Api.HealthChecks;

public sealed class FfmpegHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var (available, version) = await ProbeAsync(cancellationToken);

        return available
            ? HealthCheckResult.Healthy(version)
            : new HealthCheckResult(context.Registration.FailureStatus, "FFmpeg not found on PATH");
    }

    internal static async Task<(bool Available, string? Version)> ProbeAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            process.Start();

            var output = await process.StandardOutput.ReadLineAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0 && output is not null)
            {
                var version = output.Contains("version")
                    ? output.Split(' ').SkipWhile(s => s != "version").Skip(1).FirstOrDefault() ?? "unknown"
                    : "unknown";
                return (true, version);
            }

            return (false, null);
        }
        catch
        {
            return (false, null);
        }
    }
}
