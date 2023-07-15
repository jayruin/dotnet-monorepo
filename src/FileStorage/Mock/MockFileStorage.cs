using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileStorage.Mock;

public sealed class MockFileStorage : IFileStorage
{
    private readonly string _separator;

    private readonly Dictionary<string, byte[]> _files = new();

    private readonly HashSet<string> _directories = new();

    public MockFileStorage() : this("/")
    {
    }

    public MockFileStorage(string separator)
    {
        _separator = separator;
    }

    public IDirectory GetDirectory(params string[] paths)
    {
        return new MockDirectory(this, JoinPaths(paths));
    }

    public IFile GetFile(params string[] paths)
    {
        return new MockFile(this, JoinPaths(paths));
    }

    internal string JoinPaths(params string[] paths)
    {
        return string.Join(_separator, paths.Where(p => !string.IsNullOrEmpty(p)));
    }

    internal string[] SplitPath(string path)
    {
        return path.Split(_separator);
    }

    internal void CreateDirectory(string path)
    {
        if (IsRootPath(path)) return;
        string[] pathParts = SplitPath(path);
        for (int i = 0; i < pathParts.Length; i++)
        {
            string subPath = JoinPaths(pathParts[..^i]);
            if (FileExists(subPath))
            {
                throw new FileStorageException();
            }
            _directories.Add(subPath);
        }
        _directories.Add(path);
    }

    internal void DeleteDirectory(string path)
    {
        EnsureDirectoryExists(path);
        _files.Keys
            .Where(p => p.StartsWith(path))
            .ToList()
            .ForEach(f => _files.Remove(f));
        _directories.RemoveWhere(d => d.StartsWith(path));
    }

    internal void DeleteFile(string path)
    {
        _files.Remove(path);
    }

    internal IEnumerable<MockFile> EnumerateFiles(string path)
    {
        EnsureDirectoryExists(path);
        foreach (string filePath in _files.Keys)
        {
            if (IsChildPath(path, filePath))
            {
                yield return new MockFile(this, filePath);
            }
        }
    }

    internal IEnumerable<MockDirectory> EnumerateDirectories(string path)
    {
        EnsureDirectoryExists(path);
        foreach (string directoryPath in _directories)
        {
            if (IsChildPath(path, directoryPath))
            {
                yield return new MockDirectory(this, directoryPath);
            }
        }
    }

    internal Stream OpenRead(string path)
    {
        EnsureFileExists(path);
        return new MemoryStream(_files[path], false);
    }

    internal Stream OpenWrite(string path)
    {
        string[] pathParts = SplitPath(path);
        if (pathParts.Length > 1)
        {
            EnsureDirectoryExists(JoinPaths(pathParts[..^1]));
        }
        return new MockWritableFileStream(path, _files);
    }

    internal bool DirectoryExists(string path)
    {
        if (IsRootPath(path)) return true;
        return _directories.Contains(path);
    }

    internal bool FileExists(string path)
    {
        return _files.ContainsKey(path);
    }

    private void EnsureDirectoryExists(string path)
    {
        if (!DirectoryExists(path))
        {
            throw new FileStorageException();
        }
    }

    private void EnsureFileExists(string path)
    {
        if (!FileExists(path))
        {
            throw new FileStorageException();
        }
    }

    private static bool IsRootPath(string path)
    {
        return string.IsNullOrEmpty(path);
    }

    private bool IsChildPath(string parentPath, string path)
    {
        if (IsRootPath(parentPath))
        {
            int count = 0;
            int n = 0;
            while ((n = path.IndexOf(_separator, n, StringComparison.Ordinal)) != -1)
            {
                n += _separator.Length;
                count += 1;
            }
            return count == 0;
        }
        return path.StartsWith(parentPath) && SplitPath(path)[SplitPath(parentPath).Length..].Length == 1;
    }

    private class MockWritableFileStream : Stream
    {
        private readonly MemoryStream _stream;

        private readonly string _path;

        private readonly Dictionary<string, byte[]> _files;

        public MockWritableFileStream(string path, Dictionary<string, byte[]> files)
        {
            _stream = new MemoryStream();
            _path = path;
            _files = files;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _files[_path] = _stream.ToArray();
                _stream.Dispose();
            }
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override long Position { get => _stream.Position; set => _stream.Position = value; }

        public override void Flush() => _stream.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

        public override void SetLength(long value) => _stream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);
    }
}
