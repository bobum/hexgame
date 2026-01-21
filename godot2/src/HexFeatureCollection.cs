using Godot;

/// <summary>
/// Collection of feature prefabs for a single density level.
/// Ported exactly from Catlike Coding Hex Map Tutorial 9.
/// </summary>
[System.Serializable]
public struct HexFeatureCollection
{
    /// <summary>
    /// Array of prefab scenes for this collection.
    /// In Unity: Transform[] prefabs
    /// In Godot: PackedScene[] prefabs
    /// </summary>
    public PackedScene[] Prefabs;

    /// <summary>
    /// Selects a prefab based on a normalized choice value [0, 1).
    /// </summary>
    /// <param name="choice">Value from hash grid, range [0, 0.999)</param>
    /// <returns>Instantiated Node3D, or null if no prefabs</returns>
    public Node3D? Pick(float choice)
    {
        if (Prefabs == null || Prefabs.Length == 0)
        {
            return null;
        }
        return Prefabs[(int)(choice * Prefabs.Length)].Instantiate<Node3D>();
    }
}
