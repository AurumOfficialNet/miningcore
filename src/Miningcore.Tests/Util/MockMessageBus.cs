using Miningcore.Messaging;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace Miningcore.Tests.Util;

public class MockMessageBus : IMessageBus, IDisposable
{
    public void SendMessage<T>(T message)
    {
        // Mock implementation - does nothing
    }

    public void RegisterScheduler<T>(IScheduler scheduler, string name)
    {
        // Mock implementation - does nothing
    }

    public IObservable<T> Listen<T>(string pattern)
    {
        // Mock implementation - returns empty observable
        return new Subject<T>().AsObservable();
    }

    public IObservable<T> ListenIncludeLatest<T>(string pattern)
    {
        // Mock implementation - returns empty observable
        return new Subject<T>().AsObservable();
    }

    public bool IsRegistered(Type type, string name)
    {
        // Mock implementation - returns false
        return false;
    }

    public IDisposable RegisterMessageSource<T>(IObservable<T> source, string name)
    {
        // Mock implementation - returns empty disposable
        return System.Reactive.Disposables.Disposable.Empty;
    }

    public void Dispose()
    {
        // Mock implementation - does nothing
    }

    public void SendMessage<T>(T message, string contract = null)
    {
        // Mock implementation - does nothing
    }
}
