namespace HexGame.Utilities;

/// <summary>
/// Interface for objects that can be pooled for reuse.
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// Called when the object is acquired from the pool.
    /// </summary>
    void OnGetFromPool();

    /// <summary>
    /// Called when the object is returned to the pool.
    /// </summary>
    void OnReturnToPool();

    /// <summary>
    /// Resets the object to its initial state for reuse.
    /// </summary>
    void Reset();
}
