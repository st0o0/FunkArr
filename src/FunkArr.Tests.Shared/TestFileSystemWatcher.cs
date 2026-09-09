using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Abstractions;

namespace FunkArr.Tests.Shared;

public sealed class TestFileSystemWatcher : IFileSystemWatcher
{
    public IFileSystem FileSystem { get; set; } = null!;
    public bool EnableRaisingEvents { get; set; }
    public string Filter { get; set; } = "*";
    public Collection<string> Filters { get; } = [];
    public bool IncludeSubdirectories { get; set; }
    public int InternalBufferSize { get; set; } = 8192;
    public NotifyFilters NotifyFilter { get; set; }
    public string Path { get; set; } = "";
    public ISynchronizeInvoke? SynchronizingObject { get; set; }
    public ISite? Site { get; set; }
    public IContainer? Container => null;

    public event FileSystemEventHandler? Created;
    public event FileSystemEventHandler? Changed;
    public event FileSystemEventHandler? Deleted;
    public event RenamedEventHandler? Renamed;
    public event ErrorEventHandler? Error;

    public void SimulateCreated(string fullPath) =>
        Created?.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Created, System.IO.Path.GetDirectoryName(fullPath)!, System.IO.Path.GetFileName(fullPath)));

    public void SimulateDeleted(string fullPath) =>
        Deleted?.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Deleted, System.IO.Path.GetDirectoryName(fullPath)!, System.IO.Path.GetFileName(fullPath)));

    public void SimulateChanged(string fullPath) =>
        Changed?.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Changed, System.IO.Path.GetDirectoryName(fullPath)!, System.IO.Path.GetFileName(fullPath)));

    public IWaitForChangedResult WaitForChanged(WatcherChangeTypes changeType) =>
        throw new NotSupportedException();

    public IWaitForChangedResult WaitForChanged(WatcherChangeTypes changeType, int timeout) =>
        throw new NotSupportedException();

    public IWaitForChangedResult WaitForChanged(WatcherChangeTypes changeType, TimeSpan timeout) =>
        throw new NotSupportedException();

    public void BeginInit() { }
    public void EndInit() { }

    public void Dispose() { }
}
