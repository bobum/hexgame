using Godot;

/// <summary>
/// Provides hash values for deterministic feature placement.
/// Ported exactly from Catlike Coding Hex Map Tutorial 9.
/// </summary>
public struct HexHash
{
    /// <summary>Urban feature threshold.</summary>
    public float a;

    /// <summary>Farm feature threshold.</summary>
    public float b;

    /// <summary>Plant feature threshold.</summary>
    public float c;

    /// <summary>Prefab variant choice.</summary>
    public float d;

    /// <summary>Feature rotation (0-360 degrees when multiplied by 360).</summary>
    public float e;

    /// <summary>
    /// Creates a new HexHash with 5 random values in range [0, 0.999).
    /// The 0.999 multiplier prevents array index overflow when Random.value = 1.
    /// </summary>
    public static HexHash Create()
    {
        HexHash hash;
        hash.a = GD.Randf() * 0.999f;
        hash.b = GD.Randf() * 0.999f;
        hash.c = GD.Randf() * 0.999f;
        hash.d = GD.Randf() * 0.999f;
        hash.e = GD.Randf() * 0.999f;
        return hash;
    }
}
