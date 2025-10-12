using System;
using System.Collections.Generic;
using System.IO;
using DevCardsManager.Extensions;
using DevCardsManager.ViewModels;

namespace DevCardsManager.Services;

public sealed class DirectoryWatcher
{
    private readonly Logger _logger;
    private readonly Dictionary<string, FileSystemWatcher> _dirNameToFsWatcherMap = new();

    public event Action<string?>? DirectoryChanged;

    public DirectoryWatcher(Logger logger)
    {
        _logger = logger;
    }

    public void SetDirectory(string dirName, string path)
    {
        var newPath = path.ToOsSpecificDirectorySeparatorChar();

        if (_dirNameToFsWatcherMap.TryGetValue(dirName, out var oldFsWatcher))
        {
            oldFsWatcher.Created -= FileSystemWatcher_Changed;
            oldFsWatcher.Changed -= FileSystemWatcher_Changed;
            oldFsWatcher.Deleted -= FileSystemWatcher_Changed;
            oldFsWatcher.Error -= FileSystemWatcher_OnError;
            oldFsWatcher.Dispose();
            _dirNameToFsWatcherMap.Remove(dirName);
        }

        if (Directory.Exists(newPath))
        {
            var newFsWatcher = new FileSystemWatcher(newPath, "*.bin");
            newFsWatcher.EnableRaisingEvents = true;
            newFsWatcher.Created += FileSystemWatcher_Changed;
            newFsWatcher.Changed += FileSystemWatcher_Changed;
            newFsWatcher.Deleted += FileSystemWatcher_Changed;
            newFsWatcher.Error += FileSystemWatcher_OnError;
            _dirNameToFsWatcherMap.Add(dirName, newFsWatcher);

            DirectoryChanged?.Invoke(newPath);
            return;
        }

        DirectoryChanged?.Invoke(null);
    }

    private void FileSystemWatcher_OnError(object sender, ErrorEventArgs args)
    {
        _logger.LogException(args.GetException());
    }

    private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
    {
        var fsWatcher = (FileSystemWatcher) sender;
        _logger.LogTrace($"File system: Path: {fsWatcher.Path}, File: {e.FullPath}, Action: {e.ChangeType}");

        DirectoryChanged?.Invoke(fsWatcher.Path);
    }
}