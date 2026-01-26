using Godot;

/// <summary>
/// Defines hexagon geometry constants.
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

    // Tutorial 8: Water constants (shared with rivers)
    public const float WaterElevationOffset = -0.5f;
    public const float WaterFactor = 0.6f;
    public const float WaterBlendFactor = 1f - WaterFactor;

    // Tutorial 7: Road constants
    public const float RoadElevationOffset = 0.1f;  // Slight offset to prevent z-fighting

    // Tutorial 9: Hash grid constants for feature placement
    public const int HashGridSize = 256;
    public const float HashGridScale = 0.25f;

    private static HexHash[] _hashGrid = null!;

    // Tutorial 10: Wall constants
    public const float WallHeight = 4f;
    public const float WallYOffset = -1f;
    public const float WallThickness = 0.75f;
    public static float WallElevationOffset => VerticalTerraceStepSize;
    public const float WallTowerThreshold = 0.5f;

    // Tutorial 11: Bridge constants
    public const float BridgeDesignLength = 7f;

    // Procedural noise generation constants
    public const int NoiseTextureSize = 256;

    public const float NoiseFrequency = 0.05f;

    public const int NoiseChannelOffsetR = 0;
    public const int NoiseChannelOffsetG = 100;
    public const int NoiseChannelOffsetB = 200;
    public const int NoiseChannelOffsetA = 300;

    // Noise texture for perturbation - must be initialized before use
    public static Image? NoiseSource;

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
    ///
    /// NOTE: Direction names (NE, SE, etc.) are relative to Unity's coordinate system
    /// where +Z is forward. In Godot where -Z is forward/north, the visual directions
    /// are rotated 180° from the names (code's "SE" appears visually as "NE", etc.)
    /// The code is internally consistent - just the names don't match visual reality.
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

    // Tutorial 8: Water corner methods

    /// <summary>
    /// Returns the first water corner for a direction.
    /// Water corners are scaled by WaterFactor (0.6) to create a smaller water hexagon.
    /// </summary>
    public static Vector3 GetFirstWaterCorner(HexDirection direction)
    {
        return Corners[(int)direction] * WaterFactor;
    }

    /// <summary>
    /// Returns the second water corner for a direction.
    /// Water corners are scaled by WaterFactor (0.6) to create a smaller water hexagon.
    /// </summary>
    public static Vector3 GetSecondWaterCorner(HexDirection direction)
    {
        return Corners[(int)direction + 1] * WaterFactor;
    }

    /// <summary>
    /// Returns the bridge between water corners for a direction.
    /// Used for shore connections between water and land.
    /// </summary>
    public static Vector3 GetWaterBridge(HexDirection direction)
    {
        return (Corners[(int)direction] + Corners[(int)direction + 1]) * WaterBlendFactor;
    }

    /// <summary>
    /// Samples the noise texture at a world position using bilinear interpolation.
    /// Returns RGBA values as Vector4.
    /// </summary>
    public static Vector4 SampleNoise(Vector3 position)
    {
        if (NoiseSource == null)
        {
            return new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
        }

        int width = NoiseSource.GetWidth();
        int height = NoiseSource.GetHeight();

        // Scale position by NoiseScale and wrap to texture coordinates
        float u = position.X * NoiseScale;
        float v = position.Z * NoiseScale;

        // Wrap to 0-1 range (handle negative values)
        u = ((u % 1f) + 1f) % 1f;
        v = ((v % 1f) + 1f) % 1f;

        // Scale to texture size
        float x = u * width;
        float y = v * height;

        // Get integer coordinates and fractional parts for bilinear interpolation
        int x0 = (int)x;
        int y0 = (int)y;
        int x1 = (x0 + 1) % width;
        int y1 = (y0 + 1) % height;

        float fx = x - x0;
        float fy = y - y0;

        // Sample four neighboring pixels
        Color c00 = NoiseSource.GetPixel(x0, y0);
        Color c10 = NoiseSource.GetPixel(x1, y0);
        Color c01 = NoiseSource.GetPixel(x0, y1);
        Color c11 = NoiseSource.GetPixel(x1, y1);

        // Bilinear interpolation
        Color c0 = c00.Lerp(c10, fx);
        Color c1 = c01.Lerp(c11, fx);
        Color result = c0.Lerp(c1, fy);

        return new Vector4(result.R, result.G, result.B, result.A);
    }

    /// <summary>
    /// Perturbs a position using noise-based displacement.
    /// Only X and Z are perturbed; Y is not modified by this method.
    /// </summary>
    public static Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = SampleNoise(position);
        position.X += (sample.X * 2f - 1f) * CellPerturbStrength;
        position.Z += (sample.Z * 2f - 1f) * CellPerturbStrength;
        return position;
    }

    // Tutorial 10: Wall methods

    /// <summary>
    /// Interpolates wall position along the edge between near and far positions.
    /// XZ is averaged, Y selects based on which side is lower and applies offset.
    /// </summary>
    public static Vector3 WallLerp(Vector3 near, Vector3 far)
    {
        near.X += (far.X - near.X) * 0.5f;
        near.Z += (far.Z - near.Z) * 0.5f;
        float v = near.Y < far.Y ? WallElevationOffset : (1f - WallElevationOffset);
        near.Y += (far.Y - near.Y) * v + WallYOffset;
        return near;
    }

    /// <summary>
    /// Calculates the offset vector for wall thickness perpendicular to the wall direction.
    /// Y component is zeroed to keep wall tops flat.
    /// </summary>
    public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far)
    {
        Vector3 offset;
        offset.X = far.X - near.X;
        offset.Y = 0f;
        offset.Z = far.Z - near.Z;
        return offset.Normalized() * (WallThickness * 0.5f);
    }

    // Tutorial 9: Hash grid methods

    /// <summary>
    /// Initializes the hash grid with the given seed.
    /// Must be called before sampling. Uses GD.Seed for deterministic generation.
    /// </summary>
    public static void InitializeHashGrid(int seed)
    {
        _hashGrid = new HexHash[HashGridSize * HashGridSize];
        GD.Seed((ulong)seed);

        for (int i = 0; i < _hashGrid.Length; i++)
        {
            _hashGrid[i] = HexHash.Create();
        }
    }

    /// <summary>
    /// Samples the hash grid at a world position.
    /// Handles negative coordinates via modulo wraparound.
    /// </summary>
    public static HexHash SampleHashGrid(Vector3 position)
    {
        int x = (int)(position.X * HashGridScale) % HashGridSize;
        if (x < 0) x += HashGridSize;
        int z = (int)(position.Z * HashGridScale) % HashGridSize;
        if (z < 0) z += HashGridSize;
        return _hashGrid[x + z * HashGridSize];
    }
}
