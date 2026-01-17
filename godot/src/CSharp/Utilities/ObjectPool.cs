namespace HexGame.Utilities;

/// <summary>
/// Generic object pool for reusing objects instead of creating/destroying them.
/// Reduces garbage collection pressure for frequently used objects.
/// </summary>
/// <typeparam name="T">Type of objects to pool. Must have parameterless constructor.</typeparam>
public class ObjectPool<T> where T : class, new()
{
    private readonly Stack<T> _available = new();
    private readonly HashSet<T> _active = new();
    private readonly Action<T>? _resetAction;
    private readonly int _maxSize;
    private readonly object _lock = new();

    // Statistics
    private int _created;
    private int _reused;
    private int _peak;

    /// <summary>
    /// Creates a new object pool.
    /// </summary>
    /// <param name="resetAction">Optional action to reset objects before reuse.</param>
    /// <param name="maxSize">Maximum pool size (prevents unbounded growth).</param>
    public ObjectPool(Action<T>? resetAction = null, int maxSize = 1000)
    {
        _resetAction = resetAction;
        _maxSize = maxSize;
    }

    /// <summary>
    /// Acquires an object from the pool or creates a new one if empty.
    /// </summary>
    /// <returns>An object ready for use.</returns>
    public T Acquire()
    {
        lock (_lock)
        {
            T obj;

            if (_available.Count > 0)
            {
                obj = _available.Pop();
                _reused++;
            }
            else
            {
                obj = new T();
                _created++;
            }

            _active.Add(obj);
            _peak = Math.Max(_peak, _active.Count);

            // Notify poolable objects
            if (obj is IPoolable poolable)
            {
                poolable.OnGetFromPool();
            }

            return obj;
        }
    }

    /// <summary>
    /// Returns an object to the pool for later reuse.
    /// </summary>
    /// <param name="obj">The object to return.</param>
    public void Release(T obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        lock (_lock)
        {
            if (!_active.Contains(obj))
            {
                GD.PushWarning("ObjectPool: releasing object not from this pool");
                return;
            }

            _active.Remove(obj);

            // Reset the object
            if (obj is IPoolable poolable)
            {
                poolable.OnReturnToPool();
                poolable.Reset();
            }
            else
            {
                _resetAction?.Invoke(obj);
            }

            // Only keep up to max size
            if (_available.Count < _maxSize)
            {
                _available.Push(obj);
            }
        }
    }

    /// <summary>
    /// Releases all active objects back to the pool.
    /// </summary>
    public void ReleaseAll()
    {
        lock (_lock)
        {
            foreach (var obj in _active)
            {
                if (obj is IPoolable poolable)
                {
                    poolable.OnReturnToPool();
                    poolable.Reset();
                }
                else
                {
                    _resetAction?.Invoke(obj);
                }

                if (_available.Count < _maxSize)
                {
                    _available.Push(obj);
                }
            }
            _active.Clear();
        }
    }

    /// <summary>
    /// Pre-warms the pool with objects for better performance at runtime.
    /// </summary>
    /// <param name="count">Number of objects to pre-create.</param>
    public void Prewarm(int count)
    {
        lock (_lock)
        {
            for (int i = 0; i < count && _available.Count < _maxSize; i++)
            {
                var obj = new T();
                _available.Push(obj);
                _created++;
            }
        }
    }

    /// <summary>
    /// Clears the pool entirely.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _available.Clear();
            _active.Clear();
        }
    }

    /// <summary>
    /// Gets pool statistics.
    /// </summary>
    public PoolStats GetStats()
    {
        lock (_lock)
        {
            int total = _created + _reused;
            return new PoolStats(
                Available: _available.Count,
                Active: _active.Count,
                Created: _created,
                Reused: _reused,
                Peak: _peak,
                ReuseRate: total > 0 ? (float)_reused / total : 0f
            );
        }
    }

    /// <summary>
    /// Gets the number of active objects.
    /// </summary>
    public int ActiveCount
    {
        get
        {
            lock (_lock)
            {
                return _active.Count;
            }
        }
    }

    /// <summary>
    /// Gets the number of available objects.
    /// </summary>
    public int AvailableCount
    {
        get
        {
            lock (_lock)
            {
                return _available.Count;
            }
        }
    }

    /// <summary>
    /// Resets statistics counters.
    /// </summary>
    public void ResetStats()
    {
        lock (_lock)
        {
            _created = 0;
            _reused = 0;
            _peak = 0;
        }
    }
}

/// <summary>
/// Statistics about an object pool.
/// </summary>
/// <param name="Available">Number of objects available for reuse.</param>
/// <param name="Active">Number of objects currently in use.</param>
/// <param name="Created">Total objects created.</param>
/// <param name="Reused">Total objects reused from pool.</param>
/// <param name="Peak">Peak number of active objects.</param>
/// <param name="ReuseRate">Ratio of reused to total acquisitions.</param>
public readonly record struct PoolStats(
    int Available,
    int Active,
    int Created,
    int Reused,
    int Peak,
    float ReuseRate
);
