using System.IO.Abstractions;
using FunkArr.Api.HealthChecks;
using FunkArr.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FunkArr.Api.Tests.HealthChecks;

public sealed class DirectoryHealthCheckTests
{
    [Fact]
    public async Task Returns_healthy_for_writable_directory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"funkarr-hc-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var (dataPaths, dataFiles) = CreateServices(tempDir);
            var check = new DirectoryHealthCheck(dataPaths, dataFiles, p => p.DataRoot);
            var context = CreateContext(HealthStatus.Unhealthy);

            var result = await check.CheckHealthAsync(context);

            Assert.Equal(HealthStatus.Healthy, result.Status);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task Returns_unhealthy_for_nonexistent_directory()
    {
        var (dataPaths, dataFiles) = CreateServices("/nonexistent/path/that/does/not/exist");
        var check = new DirectoryHealthCheck(dataPaths, dataFiles, p => p.DataRoot);
        var context = CreateContext(HealthStatus.Unhealthy);

        var result = await check.CheckHealthAsync(context);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("not writable", result.Description);
    }

    [Fact]
    public void Probe_returns_writable_true_for_existing_directory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"funkarr-hc-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var dataFiles = new DataFiles(new FileSystem(), NullLogger<DataFiles>.Instance);
            var (writable, fullPath) = DirectoryHealthCheck.Probe(tempDir, dataFiles);

            Assert.True(writable);
            Assert.Equal(Path.GetFullPath(tempDir), fullPath);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Probe_returns_writable_false_for_nonexistent_directory()
    {
        var dataFiles = new DataFiles(new FileSystem(), NullLogger<DataFiles>.Instance);
        var (writable, _) = DirectoryHealthCheck.Probe("/nonexistent/path", dataFiles);

        Assert.False(writable);
    }

    private static (DataPaths, IDataFiles) CreateServices(string dataPath)
    {
        var funkArrOptions = Options.Create(new FunkArrOptions { DataPath = dataPath });
        var downloadOptions = Options.Create(new DownloadOptions { Path = Path.Combine(dataPath, "downloads") });
        var dataPaths = new DataPaths(funkArrOptions, downloadOptions);
        var dataFiles = new DataFiles(new FileSystem(), NullLogger<DataFiles>.Instance);
        return (dataPaths, dataFiles);
    }

    private static HealthCheckContext CreateContext(HealthStatus failureStatus) =>
        new()
        {
            Registration = new HealthCheckRegistration(
                "test", _ => null!, failureStatus, null),
        };
}
