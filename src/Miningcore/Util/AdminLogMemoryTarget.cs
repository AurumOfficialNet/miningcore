using NLog;
using NLog.Targets;
using System.Collections.Concurrent;

namespace Miningcore.Util;

/// <summary>
/// NLog target that retains the last <see cref="Capacity"/> log events in memory.
/// Designed as a singleton registered with Autofac so admin API endpoints can surface
/// recent Warn/Error/Fatal events without file I/O.
/// </summary>
[Target("AdminMemory")]
public sealed class AdminLogMemoryTarget : Target
{
    private readonly ConcurrentQueue<LogEventInfo> events = new();

    public int Capacity { get; set; } = 200;

    protected override void Write(LogEventInfo logEvent)
    {
        events.Enqueue(logEvent);

        // Evict oldest when over capacity
        while(events.Count > Capacity)
            events.TryDequeue(out _);
    }

    /// <summary>Returns a snapshot of retained events, newest-first.</summary>
    public LogEventInfo[] GetEvents() =>
        events.Reverse().ToArray();
}
