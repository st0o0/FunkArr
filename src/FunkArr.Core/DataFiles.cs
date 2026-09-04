using System.IO.Abstractions;
using Microsoft.Extensions.Logging;

namespace FunkArr.Core;

public sealed class DataFiles(IFileSystem fs, ILogger<DataFiles> log) : IDataFiles
{
    private const UnixFileMode _dirMode =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
        | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
        | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

    private const UnixFileMode _fileMode =
        UnixFileMode.UserRead | UnixFileMode.UserWrite
        | UnixFileMode.GroupRead | UnixFileMode.GroupWrite
        | UnixFileMode.OtherRead | UnixFileMode.OtherWrite;

    public void CreateDirectory(string path)
    {
        fs.Directory.CreateDirectory(path);
        SetDirPermissions(path);
    }

    public void Remove(string path)
    {
        try
        {
            if (fs.File.Exists(path))
            {
                fs.File.Delete(path);
            }
            else if (fs.Directory.Exists(path))
            {
                fs.Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Failed to remove {Path}", path);
        }
    }

    public void Move(string source, string destination)
    {
        fs.File.Move(source, destination, overwrite: true);
        SetFilePermissions(destination);
    }

    public void ReplaceDirectory(string source, string target)
    {
        var parent = fs.Path.GetDirectoryName(target)!;
        var oldDir = fs.Path.Join(parent, $".old-{Guid.NewGuid():N}");

        try
        {
            if (fs.Directory.Exists(target))
            {
                fs.Directory.Move(target, oldDir);
            }

            fs.Directory.CreateDirectory(parent);
            fs.Directory.Move(source, target);
            SetDirPermissions(target);

            if (fs.Directory.Exists(oldDir))
            {
                fs.Directory.Delete(oldDir, recursive: true);
            }
        }
        catch (Exception) when (fs.Directory.Exists(oldDir) && !fs.Directory.Exists(target))
        {
            fs.Directory.Move(oldDir, target);
            throw;
        }
    }

    public string ReadText(string path) =>
        fs.File.ReadAllText(path);

    public void WriteText(string path, string content)
    {
        fs.File.WriteAllText(path, content);
        SetFilePermissions(path);
    }

    public void WriteAtomic(string path, string content)
    {
        var dir = fs.Path.GetDirectoryName(path)!;
        var tempPath = fs.Path.Join(dir, $".tmp-{Guid.NewGuid():N}");

        try
        {
            fs.File.WriteAllText(tempPath, content);
            fs.File.Move(tempPath, path, overwrite: true);
            SetFilePermissions(path);
        }
        catch
        {
            if (fs.File.Exists(tempPath))
            {
                fs.File.Delete(tempPath);
            }

            throw;
        }
    }

    public bool Exists(string path) =>
        fs.File.Exists(path) || fs.Directory.Exists(path);

    public string[] ListFiles(string directory, string pattern) =>
        fs.Directory.Exists(directory) ? fs.Directory.GetFiles(directory, pattern) : [];

    public bool CanWrite(string directory)
    {
        if (!fs.Directory.Exists(directory))
        {
            return false;
        }

        var testFile = fs.Path.Join(directory, $".funkarr-write-test-{Guid.NewGuid():N}");

        try
        {
            fs.File.WriteAllText(testFile, "test");
            fs.File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public IFileSystemWatcher Watch(string directory, string filter)
    {
        CreateDirectory(directory);

        var watcher = fs.FileSystemWatcher.New(directory, filter);
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
        watcher.EnableRaisingEvents = true;

        return watcher;
    }

    private void SetDirPermissions(string path)
    {
        if (OperatingSystem.IsLinux())
        {
            fs.DirectoryInfo.New(path).UnixFileMode = _dirMode;
        }
    }

    private void SetFilePermissions(string path)
    {
        if (OperatingSystem.IsLinux())
        {
            fs.FileInfo.New(path).UnixFileMode = _fileMode;
        }
    }
}
