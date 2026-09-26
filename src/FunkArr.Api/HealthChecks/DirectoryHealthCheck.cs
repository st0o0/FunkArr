using FunkArr.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FunkArr.Api.HealthChecks;

public sealed class DirectoryHealthCheck(
    DataPaths dataPaths,
    IDataFiles dataFiles,
    Func<DataPaths, string> pathSelector) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var path = pathSelector(dataPaths);
        var (writable, fullPath) = Probe(path, dataFiles);

        var result = writable
            ? HealthCheckResult.Healthy(fullPath)
            : new HealthCheckResult(context.Registration.FailureStatus,
                $"Directory not writable or does not exist: {fullPath}");

        return Task.FromResult(result);
    }

    internal static (bool Writable, string FullPath) Probe(string path, IDataFiles dataFiles)
    {
        var fullPath = Path.GetFullPath(path);
        return (dataFiles.CanWrite(fullPath), fullPath);
    }
}
