using System.IO.Abstractions;

namespace FunkArr.Core;

public interface IDataFiles
{
    void CreateDirectory(string path);
    void Remove(string path);
    void Move(string source, string destination);
    void ReplaceDirectory(string source, string target);
    string ReadText(string path);
    void WriteText(string path, string content);
    void WriteAtomic(string path, string content);
    bool Exists(string path);
    string[] ListFiles(string directory, string pattern);
    bool CanWrite(string directory);
    IFileSystemWatcher Watch(string directory, string filter);
}
