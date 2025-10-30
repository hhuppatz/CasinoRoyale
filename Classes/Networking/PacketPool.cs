using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace CasinoRoyale.Classes.Networking;

// Object pool for reusing network packets to reduce garbage collection pressure
public class PacketPool<T>(Func<T> factory = null, Action<T> resetAction = null, int maxPoolSize = 100) where T : class, new()
{
    private readonly ConcurrentQueue<T> _pool = new();
    private readonly Func<T> _factory = factory ?? (() => new T());
    private readonly Action<T> _resetAction = resetAction;
    private readonly int _maxPoolSize = maxPoolSize;
    private volatile int _currentSize = 0;

    // Get a packet from the pool or create a new one
    public T Rent()
    {
        if (_pool.TryDequeue(out var item))
        {
            Interlocked.Decrement(ref _currentSize);
            return item;
        }
        
        return _factory();
    }

    // Return a packet to the pool for reuse
    public void Return(T item)
    {
        if (item == null || _currentSize >= _maxPoolSize)
            return;

        // Reset the item state if a reset action is provided
        _resetAction?.Invoke(item);
        
        _pool.Enqueue(item);
        Interlocked.Increment(ref _currentSize);
    }

    // Get the current number of items in the pool
    public int Count => _currentSize;

    // Clear all items from the pool
    public void Clear()
    {
        while (_pool.TryDequeue(out _))
        {
            Interlocked.Decrement(ref _currentSize);
        }
    }
}

// Centralized packet pool manager for all network packets
public static class PacketPoolManager
{
    private static readonly Dictionary<Type, object> _pools = [];
    private static readonly object _poolsLock = new();

    // Get a packet pool for the specified type
    public static PacketPool<T> GetPool<T>() where T : class, new()
    {
        lock (_poolsLock)
        {
            if (!_pools.TryGetValue(typeof(T), out var pool))
            {
                pool = new PacketPool<T>();
                _pools[typeof(T)] = pool;
            }
            return (PacketPool<T>)pool;
        }
    }

    // Get a packet from the appropriate pool
    public static T Rent<T>() where T : class, new()
    {
        return GetPool<T>().Rent();
    }

    // Return a packet to the appropriate pool
    public static void Return<T>(T packet) where T : class, new()
    {
        GetPool<T>().Return(packet);
    }

    // Clear all pools
    public static void ClearAll()
    {
        lock (_poolsLock)
        {
            foreach (var pool in _pools.Values)
            {
                if (pool is IDisposable disposable)
                    disposable.Dispose();
            }
            _pools.Clear();
        }
    }
}

// Extension methods for easier packet pooling
public static class PacketPoolExtensions
{
    // Rent a packet, use it with the provided action, then return it to the pool
    public static void UsePooledPacket<T>(Action<T> action) where T : class, new()
    {
        var packet = PacketPoolManager.Rent<T>();
        try
        {
            action(packet);
        }
        finally
        {
            PacketPoolManager.Return(packet);
        }
    }

    // Rent a packet, use it with the provided function, then return it to the pool
    public static TResult UsePooledPacket<T, TResult>(Func<T, TResult> func) where T : class, new()
    {
        var packet = PacketPoolManager.Rent<T>();
        try
        {
            return func(packet);
        }
        finally
        {
            PacketPoolManager.Return(packet);
        }
    }
}
