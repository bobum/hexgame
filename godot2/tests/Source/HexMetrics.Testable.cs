using Godot;

/// <summary>
/// Defines hexagon geometry constants.
/// Testable version - must stay in sync with main HexMetrics.cs
/// Ported from Catlike Coding Hex Map Tutorials 1-5.
/// </summary>
public static class HexMetrics
{
    public const float OuterRadius = 10f;

    // Tutorial 5: Chunk system constants
    public const int ChunkSizeX = 5;
    public const int ChunkSizeZ = 5;

    public const float OuterToInner = 0.866025404f; // sqrt(3)/2
    public const float InnerToOuter = 1f / OuterToInner;

    public const float InnerRadius = OuterRadius * OuterToInner;

    public const float SolidFactor = 0.8f;

    public const float BlendFactor = 1f - SolidFactor;

    public const float ElevationStep = 3f;

    // Tutorial 4: Irregularity constants
    public const float CellPerturbStrength = 4f;

    public const float NoiseScale = 0.003f;

    public const float ElevationPerturbStrength = 1.5f;

    // Tutorial 6: River constants
    public const float StreamBedElevationOffset = -1.75f;
    public const float RiverSurfaceElevationOffset = -0.5f;

    public const int TerracesPerSlope = 2;

    public const int TerraceSteps = TerracesPerSlope * 2 + 1;

    public const float HorizontalTerraceStepSize = 1f / TerraceSteps;

    public const float VerticalTerraceStepSize = 1f / (TerracesPerSlope + 1);

    public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
    {
        if (elevation1 == elevation2)
        {
            return HexEdgeType.Flat;
        }
        int delta = elevation2 - elevation1;
        if (delta == 1 || delta == -1)
        {
            return HexEdgeType.Slope;
        }
        return HexEdgeType.Cliff;
    }

    /// <summary>
    /// Interpolates between two positions using terrace-style stepping.
    /// Horizontal movement is linear, vertical only changes on odd steps.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: The division (step + 1) / 2 MUST use integer division.
    /// This creates the staircase pattern: steps 1,2 -> v=1, steps 3,4 -> v=2, step 5 -> v=3
    /// Do not "fix" this to (step + 1) / 2f as it would break the terrace effect.
    /// </remarks>
    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        float h = step * HorizontalTerraceStepSize;
        a.X += (b.X - a.X) * h;
        a.Z += (b.Z - a.Z) * h;
        float v = ((step + 1) / 2) * VerticalTerraceStepSize;
        a.Y += (b.Y - a.Y) * v;
        return a;
    }

    /// <summary>
    /// Interpolates between two colors using horizontal terrace step size.
    /// Color blending is linear across all steps (no staircase effect).
    /// </summary>
    public static Color TerraceLerp(Color a, Color b, int step)
    {
        float h = step * HorizontalTerraceStepSize;
        return a.Lerp(b, h);
    }

    /// <summary>
    /// Corner positions for pointy-top hexagons.
    /// 7 elements: index 6 duplicates index 0 for easy wraparound when triangulating.
    /// </summary>
    public static Vector3[] Corners =
    {
        new Vector3(0f, 0f, OuterRadius),
        new Vector3(InnerRadius, 0f, 0.5f * OuterRadius),
        new Vector3(InnerRadius, 0f, -0.5f * OuterRadius),
        new Vector3(0f, 0f, -OuterRadius),
        new Vector3(-InnerRadius, 0f, -0.5f * OuterRadius),
        new Vector3(-InnerRadius, 0f, 0.5f * OuterRadius),
        new Vector3(0f, 0f, OuterRadius)
    };

    public static Vector3 GetFirstCorner(HexDirection direction)
    {
        return Corners[(int)direction];
    }

    public static Vector3 GetSecondCorner(HexDirection direction)
    {
        return Corners[(int)direction + 1];
    }

    public static Vector3 GetFirstSolidCorner(HexDirection direction)
    {
        return Corners[(int)direction] * SolidFactor;
    }

    public static Vector3 GetSecondSolidCorner(HexDirection direction)
    {
        return Corners[(int)direction + 1] * SolidFactor;
    }

    public static Vector3 GetBridge(HexDirection direction)
    {
        return (Corners[(int)direction] + Corners[(int)direction + 1]) * BlendFactor;
    }

    /// <summary>
    /// Returns the middle point of a solid edge. Tutorial 6.
    /// Used for gentle river curve calculations.
    /// </summary>
    public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
    {
        return (Corners[(int)direction] + Corners[(int)direction + 1]) * (0.5f * SolidFactor);
    }
}
