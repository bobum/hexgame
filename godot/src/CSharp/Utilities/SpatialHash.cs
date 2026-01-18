namespace HexGame.Utilities;

/// <summary>
/// Generic spatial hash grid for O(1) point lookups and efficient range queries.
/// Used for finding units, features, or any spatially-located objects.
/// </summary>
/// <typeparam name="T">Type of objects to store in the hash.</typeparam>
public class SpatialHash<T> where T : notnull
{
    private readonly Dictionary<string, List<T>> _buckets = new();
    private readonly Dictionary<T, (float X, float Z)> _itemPositions = new();
    private readonly float _cellSize;
    private readonly object _lock = new();

    // Statistics
    private int _insertCount;
    private int _queryCount;
    private int _peakItems;

    /// <summary>
    /// Creates a spatial hash with the given cell size.
    /// Smaller cells = more memory but faster range queries.
    /// Recommended: cell size slightly larger than typical query radius.
    /// </summary>
    /// <param name="cellSize">Size of each bucket cell.</param>
    public SpatialHash(float cellSize = 2.0f)
    {
        _cellSize = cellSize;
    }

    private string GetKey(float x, float z)
    {
        int bx = (int)Mathf.Floor(x / _cellSize);
        int bz = (int)Mathf.Floor(z / _cellSize);
        return $"{bx},{bz}";
    }

    private (int X, int Z) GetBucketCoords(float x, float z)
    {
        return ((int)Mathf.Floor(x / _cellSize), (int)Mathf.Floor(z / _cellSize));
    }

    /// <summary>
    /// Inserts an item at the given position.
    /// </summary>
    /// <param name="item">The item to insert.</param>
    /// <param name="x">X world coordinate.</param>
    /// <param name="z">Z world coordinate.</param>
    public void Insert(T item, float x, float z)
    {
        lock (_lock)
        {
            // Remove from old position if already tracked
            if (_itemPositions.ContainsKey(item))
            {
                RemoveInternal(item);
            }

            string key = GetKey(x, z);
            if (!_buckets.TryGetValue(key, out var bucket))
            {
                bucket = new List<T>();
                _buckets[key] = bucket;
            }
            bucket.Add(item);
            _itemPositions[item] = (x, z);

            _insertCount++;
            _peakItems = Math.Max(_peakItems, _itemPositions.Count);
        }
    }

    /// <summary>
    /// Removes an item from the hash.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>True if the item was found and removed.</returns>
    public bool Remove(T item)
    {
        lock (_lock)
        {
            return RemoveInternal(item);
        }
    }

    private bool RemoveInternal(T item)
    {
        if (!_itemPositions.TryGetValue(item, out var pos))
        {
            return false;
        }

        string key = GetKey(pos.X, pos.Z);
        if (_buckets.TryGetValue(key, out var bucket))
        {
            bucket.Remove(item);
            if (bucket.Count == 0)
            {
                _buckets.Remove(key);
            }
        }

        _itemPositions.Remove(item);
        return true;
    }

    /// <summary>
    /// Updates an item's position.
    /// </summary>
    /// <param name="item">The item to update.</param>
    /// <param name="x">New X coordinate.</param>
    /// <param name="z">New Z coordinate.</param>
    public void Update(T item, float x, float z)
    {
        Insert(item, x, z); // Insert handles removal automatically
    }

    /// <summary>
    /// Gets all items at exact bucket position.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="z">Z coordinate.</param>
    /// <returns>List of items at that bucket.</returns>
    public List<T> GetAt(float x, float z)
    {
        lock (_lock)
        {
            _queryCount++;
            string key = GetKey(x, z);
            if (_buckets.TryGetValue(key, out var bucket))
            {
                return new List<T>(bucket);
            }
            return new List<T>();
        }
    }

    /// <summary>
    /// Gets the first item at a position, or default.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="z">Z coordinate.</param>
    /// <returns>The first item or default.</returns>
    public T? GetFirstAt(float x, float z)
    {
        lock (_lock)
        {
            _queryCount++;
            string key = GetKey(x, z);
            if (_buckets.TryGetValue(key, out var bucket) && bucket.Count > 0)
            {
                return bucket[0];
            }
            return default;
        }
    }

    /// <summary>
    /// Checks if any item exists at position.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="z">Z coordinate.</param>
    /// <returns>True if any item exists at that bucket.</returns>
    public bool HasAt(float x, float z)
    {
        lock (_lock)
        {
            string key = GetKey(x, z);
            return _buckets.TryGetValue(key, out var bucket) && bucket.Count > 0;
        }
    }

    /// <summary>
    /// Queries all items within a radius of the given point.
    /// </summary>
    /// <param name="x">Center X coordinate.</param>
    /// <param name="z">Center Z coordinate.</param>
    /// <param name="radius">Search radius.</param>
    /// <returns>List of items within the radius.</returns>
    public List<T> QueryRadius(float x, float z, float radius)
    {
        lock (_lock)
        {
            _queryCount++;
            var results = new List<T>();
            float radiusSq = radius * radius;

            var minB = GetBucketCoords(x - radius, z - radius);
            var maxB = GetBucketCoords(x + radius, z + radius);

            for (int bx = minB.X; bx <= maxB.X; bx++)
            {
                for (int bz = minB.Z; bz <= maxB.Z; bz++)
                {
                    string key = $"{bx},{bz}";
                    if (!_buckets.TryGetValue(key, out var bucket))
                    {
                        continue;
                    }

                    foreach (var item in bucket)
                    {
                        if (_itemPositions.TryGetValue(item, out var pos))
                        {
                            float dx = pos.X - x;
                            float dz = pos.Z - z;
                            if (dx * dx + dz * dz <= radiusSq)
                            {
                                results.Add(item);
                            }
                        }
                    }
                }
            }

            return results;
        }
    }

    /// <summary>
    /// Queries all items within a rectangular area.
    /// </summary>
    /// <param name="minX">Minimum X coordinate.</param>
    /// <param name="minZ">Minimum Z coordinate.</param>
    /// <param name="maxX">Maximum X coordinate.</param>
    /// <param name="maxZ">Maximum Z coordinate.</param>
    /// <returns>List of items within the rectangle.</returns>
    public List<T> QueryRect(float minX, float minZ, float maxX, float maxZ)
    {
        lock (_lock)
        {
            _queryCount++;
            var results = new List<T>();

            var minB = GetBucketCoords(minX, minZ);
            var maxB = GetBucketCoords(maxX, maxZ);

            for (int bx = minB.X; bx <= maxB.X; bx++)
            {
                for (int bz = minB.Z; bz <= maxB.Z; bz++)
                {
                    string key = $"{bx},{bz}";
                    if (!_buckets.TryGetValue(key, out var bucket))
                    {
                        continue;
                    }

                    foreach (var item in bucket)
                    {
                        if (_itemPositions.TryGetValue(item, out var pos))
                        {
                            if (pos.X >= minX && pos.X <= maxX && pos.Z >= minZ && pos.Z <= maxZ)
                            {
                                results.Add(item);
                            }
                        }
                    }
                }
            }

            return results;
        }
    }

    /// <summary>
    /// Gets all items in the hash.
    /// </summary>
    /// <returns>List of all items.</returns>
    public List<T> GetAll()
    {
        lock (_lock)
        {
            return new List<T>(_itemPositions.Keys);
        }
    }

    /// <summary>
    /// Gets the position of an item.
    /// </summary>
    /// <param name="item">The item to find.</param>
    /// <param name="position">The position if found.</param>
    /// <returns>True if the item was found.</returns>
    public bool TryGetPosition(T item, out (float X, float Z) position)
    {
        lock (_lock)
        {
            return _itemPositions.TryGetValue(item, out position);
        }
    }

    /// <summary>
    /// Checks if an item is in the hash.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <returns>True if the item is tracked.</returns>
    public bool Contains(T item)
    {
        lock (_lock)
        {
            return _itemPositions.ContainsKey(item);
        }
    }

    /// <summary>
    /// Clears all items from the hash.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _buckets.Clear();
            _itemPositions.Clear();
        }
    }

    /// <summary>
    /// Gets the number of items in the hash.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _itemPositions.Count;
            }
        }
    }

    /// <summary>
    /// Gets the number of active buckets.
    /// </summary>
    public int BucketCount
    {
        get
        {
            lock (_lock)
            {
                return _buckets.Count;
            }
        }
    }

    /// <summary>
    /// Gets statistics for debugging.
    /// </summary>
    public SpatialHashStats GetStats()
    {
        lock (_lock)
        {
            return new SpatialHashStats(
                Items: _itemPositions.Count,
                Buckets: _buckets.Count,
                InsertCount: _insertCount,
                QueryCount: _queryCount,
                PeakItems: _peakItems,
                CellSize: _cellSize
            );
        }
    }

    /// <summary>
    /// Resets statistics counters.
    /// </summary>
    public void ResetStats()
    {
        lock (_lock)
        {
            _insertCount = 0;
            _queryCount = 0;
        }
    }
}

/// <summary>
/// Statistics about a spatial hash.
/// </summary>
public readonly record struct SpatialHashStats(
    int Items,
    int Buckets,
    int InsertCount,
    int QueryCount,
    int PeakItems,
    float CellSize
);
