using System.IO.Abstractions;
using FunkArr.Core;

namespace FunkArr.Tests.Shared;

public sealed class TestDataFiles(IDataFiles inner) : IDataFiles
{
    private readonly List<TestFileSystemWatcher> _watchers = [];

    public IReadOnlyList<TestFileSystemWatcher> Watchers => _watchers;

    public void CreateDirectory(string path) => inner.CreateDirectory(path);
    public void Remove(string path) => inner.Remove(path);
    public void Move(string source, string destination) => inner.Move(source, destination);
    public void ReplaceDirectory(string source, string target) => inner.ReplaceDirectory(source, target);
    public string ReadText(string path) => inner.ReadText(path);
    public void WriteText(string path, string content) => inner.WriteText(path, content);
    public void WriteAtomic(string path, string content) => inner.WriteAtomic(path, content);
    public bool Exists(string path) => inner.Exists(path);
    public string[] ListFiles(string directory, string pattern) => inner.ListFiles(directory, pattern);
    public bool CanWrite(string directory) => inner.CanWrite(directory);

    public IFileSystemWatcher Watch(string directory, string filter)
    {
        var watcher = new TestFileSystemWatcher { Path = directory, Filter = filter };
        _watchers.Add(watcher);
        return watcher;
    }
}
