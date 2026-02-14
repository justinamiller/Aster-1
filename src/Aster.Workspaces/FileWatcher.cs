namespace Aster.Workspaces;

/// <summary>
/// Watches for file changes in a workspace directory with debouncing.
/// </summary>
public sealed class FileWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly TimeSpan _debounceInterval;
    private readonly Action<FileChangeEvent> _onChange;
    private Timer? _debounceTimer;
    private readonly object _lock = new();
    private readonly List<FileChangeEvent> _pendingEvents = new();

    public FileWatcher(string path, string filter, TimeSpan debounceInterval, Action<FileChangeEvent> onChange)
    {
        _debounceInterval = debounceInterval;
        _onChange = onChange;

        _watcher = new FileSystemWatcher(path, filter)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
        };

        _watcher.Changed += OnFileEvent;
        _watcher.Created += OnFileEvent;
        _watcher.Deleted += OnFileEvent;
        _watcher.Renamed += OnRenamedEvent;
    }

    public void Start() => _watcher.EnableRaisingEvents = true;
    public void Stop() => _watcher.EnableRaisingEvents = false;

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        var changeType = e.ChangeType switch
        {
            WatcherChangeTypes.Created => FileChangeKind.Created,
            WatcherChangeTypes.Deleted => FileChangeKind.Deleted,
            _ => FileChangeKind.Modified
        };
        EnqueueEvent(new FileChangeEvent(e.FullPath, changeType));
    }

    private void OnRenamedEvent(object sender, RenamedEventArgs e)
    {
        EnqueueEvent(new FileChangeEvent(e.OldFullPath, FileChangeKind.Deleted));
        EnqueueEvent(new FileChangeEvent(e.FullPath, FileChangeKind.Created));
    }

    private void EnqueueEvent(FileChangeEvent evt)
    {
        lock (_lock)
        {
            _pendingEvents.Add(evt);
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(FlushEvents, null, _debounceInterval, Timeout.InfiniteTimeSpan);
        }
    }

    private void FlushEvents(object? state)
    {
        List<FileChangeEvent> events;
        lock (_lock)
        {
            events = new List<FileChangeEvent>(_pendingEvents);
            _pendingEvents.Clear();
        }

        foreach (var evt in events)
            _onChange(evt);
    }

    public void Dispose()
    {
        _debounceTimer?.Dispose();
        _watcher.Dispose();
    }
}

/// <summary>
/// Represents a file change event.
/// </summary>
public sealed record FileChangeEvent(string FilePath, FileChangeKind Kind);

/// <summary>
/// Kind of file change.
/// </summary>
public enum FileChangeKind
{
    Created,
    Modified,
    Deleted
}
