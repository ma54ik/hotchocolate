using System.Collections;
using System.Collections.Concurrent;

namespace HotChocolate.AspNetCore.Subscriptions;

public class SubscriptionManager : ISubscriptionManager
{
    private readonly ConcurrentDictionary<string, ISubscriptionSession> _subs = new();
    private readonly ISocketConnection _connection;
    private bool _disposed;

    public SubscriptionManager(ISocketConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public void Register(ISubscriptionSession subscriptionSession)
    {
        if (subscriptionSession == null)
        {
            throw new ArgumentNullException(nameof(subscriptionSession));
        }

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SubscriptionManager));
        }

        if (_subs.TryAdd(subscriptionSession.Id, subscriptionSession))
        {
            subscriptionSession.Completed += (_, _) => Unregister(subscriptionSession.Id);
        }

        if (subscriptionSession.IsCompleted)
        {
            try
            {
                Unregister(subscriptionSession.Id);
            }
            catch (ObjectDisposedException)
            {
                // the manager is disposing while we were still at work.
            }
        }
    }

    public void Unregister(string subscriptionId)
    {
        if (subscriptionId == null)
        {
            throw new ArgumentNullException(nameof(subscriptionId));
        }

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SubscriptionManager));
        }

        if (_subs.TryRemove(subscriptionId, out ISubscriptionSession? subscription))
        {
            subscription.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing && !_subs.IsEmpty)
            {
                ISubscriptionSession?[] subs = _subs.Values.ToArray();
                _subs.Clear();

                for (var i = 0; i < subs.Length; i++)
                {
                    subs[i]?.Dispose();
                    subs[i] = null;
                }
            }
            _disposed = true;
        }
    }

    public IEnumerator<ISubscriptionSession> GetEnumerator()
    {
        return _subs.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
