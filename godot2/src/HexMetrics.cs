using Godot;

/// <summary>
/// Defines hexagon geometry constants.
/// Ported exactly from Catlike Coding Hex Map Tutorials 1-4.
/// </summary>
public static class HexMetrics
{
    public const float OuterRadius = 10f;

    public const float InnerRadius = OuterRadius * 0.866025404f;

    public const float SolidFactor = 0.8f;

    public const float BlendFactor = 1f - SolidFactor;

    public const float ElevationStep = 3f;

    // Tutorial 4: Irregularity constants
    public const float CellPerturbStrength = 8f;  // Tutorial value is 4f, temporarily doubled for testing

    public const float NoiseScale = 0.003f;

    public const float ElevationPerturbStrength = 1.5f;

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
}
