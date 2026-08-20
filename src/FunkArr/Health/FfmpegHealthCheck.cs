using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FunkArr.Health;

public sealed class FfmpegHealthCheck : IHealthCheck
{
    private const string FfmpegBinary = "ffmpeg";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = FfmpegBinary,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            process.Start();
            var output = await process.StandardOutput.ReadLineAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode == 0
                ? HealthCheckResult.Healthy($"FFmpeg available: {output}")
                : HealthCheckResult.Unhealthy("FFmpeg returned non-zero exit code");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("FFmpeg not found or not executable", ex);
        }
    }
}
