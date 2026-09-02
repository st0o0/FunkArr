using System.Diagnostics;

namespace FunkArr.Download;

internal sealed class FfmpegProcess : IDisposable
{
    private readonly Process _process;
    private readonly List<string> _stderrLines = [];

    private FfmpegProcess(Process process)
    {
        _process = process;
        _process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                _stderrLines.Add(e.Data);
            }
        };
    }

    public static FfmpegProcess Start(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
        };

        var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start FFmpeg process");
        var wrapper = new FfmpegProcess(process);
        process.BeginErrorReadLine();
        return wrapper;
    }

    public int ProcessId => _process.Id;

    public StreamReader StandardOutput => _process.StandardOutput;

    public async Task<int> WaitForExitAsync(CancellationToken ct = default)
    {
        await _process.WaitForExitAsync(ct);
        return _process.ExitCode;
    }

    public string GetStderrOutput() => string.Join(Environment.NewLine, _stderrLines);

    public void Kill()
    {
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // noop
        }
    }

    public void Dispose()
    {
        Kill();
        _process.Dispose();
    }
}
