namespace HexGame.Core;

/// <summary>
/// Core hex geometry constants and utilities.
/// Provides all the mathematical constants needed for hex grid calculations.
/// </summary>
/// <remarks>
/// Based on the Catlike Coding hex map tutorials with terraced terrain support.
/// Uses flat-topped hexagons with pointy sides.
/// </remarks>
public static class HexMetrics
{
    #region Hex Geometry

    /// <summary>
    /// Distance from center to corner (vertex) of the hex.
    /// </summary>
    public const float OuterRadius = 1.0f;

    /// <summary>
    /// Distance from center to edge midpoint. Equal to OuterRadius * sqrt(3)/2.
    /// </summary>
    public const float InnerRadius = OuterRadius * 0.866025404f;

    #endregion

    #region Elevation

    /// <summary>
    /// World-space height change per elevation level.
    /// </summary>
    public const float ElevationStep = 0.4f;

    /// <summary>
    /// Minimum elevation value (ocean floor/deepest water).
    /// </summary>
    public const int MinElevation = 0;

    /// <summary>
    /// Water surface elevation. Water occupies elevations 0-4.
    /// </summary>
    public const int SeaLevel = 4;

    /// <summary>
    /// Minimum land elevation (always 1 above sea level).
    /// </summary>
    public const int LandMinElevation = 5;

    /// <summary>
    /// Maximum terrain elevation (highest mountains).
    /// </summary>
    public const int MaxElevation = 13;

    #endregion

    #region Terraces (Catlike Coding Style)

    /// <summary>
    /// Number of flat terrace steps per slope between elevation levels.
    /// </summary>
    public const int TerracesPerSlope = 2;

    /// <summary>
    /// Gets the total number of terrace steps including slopes and flats.
    /// </summary>
    public static int TerraceSteps => TerracesPerSlope * 2 + 1;

    /// <summary>
    /// Gets the horizontal interpolation step size for terrace lerping.
    /// </summary>
    public static float HorizontalTerraceStepSize => 1f / TerraceSteps;

    /// <summary>
    /// Gets the vertical interpolation step size for terrace lerping.
    /// </summary>
    public static float VerticalTerraceStepSize => 1f / (TerracesPerSlope + 1);

    #endregion

    #region Blend Regions

    /// <summary>
    /// Factor for the inner solid portion of each hex cell.
    /// </summary>
    public const float SolidFactor = 0.8f;

    /// <summary>
    /// Factor for the outer blend portion where cells connect.
    /// </summary>
    public const float BlendFactor = 0.2f;

    #endregion

    #region Corner Calculations

    private static readonly Vector3[] _corners;

    static HexMetrics()
    {
        _corners = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            // Start at 30 degrees for flat-topped hex
            float angle = (Mathf.Pi / 3f) * i + Mathf.Pi / 6f;
            _corners[i] = new Vector3(
                Mathf.Cos(angle) * OuterRadius,
                0f,
                Mathf.Sin(angle) * OuterRadius
            );
        }
    }

    /// <summary>
    /// Gets the 6 corner positions for a hex (flat-topped, starting at 30 degrees).
    /// </summary>
    /// <returns>Array of 6 corner positions in local space.</returns>
    public static Vector3[] GetCorners() => _corners;

    /// <summary>
    /// Gets a specific corner by index with wrapping.
    /// </summary>
    /// <param name="index">Corner index (wraps around).</param>
    /// <returns>The corner position in local space.</returns>
    public static Vector3 GetCorner(int index)
    {
        return _corners[((index % 6) + 6) % 6];
    }

    #endregion

    #region Terrace Interpolation

    /// <summary>
    /// Performs terrace-style interpolation between two points.
    /// Horizontal movement is linear, vertical only changes on odd steps.
    /// </summary>
    /// <param name="a">Start position.</param>
    /// <param name="b">End position.</param>
    /// <param name="step">Current terrace step.</param>
    /// <returns>Interpolated position.</returns>
    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        float h = step * HorizontalTerraceStepSize;
        float v = Mathf.Floor((step + 1) / 2f) * VerticalTerraceStepSize;

        return new Vector3(
            a.X + (b.X - a.X) * h,
            a.Y + (b.Y - a.Y) * v,
            a.Z + (b.Z - a.Z) * h
        );
    }

    /// <summary>
    /// Performs terrace-style color interpolation.
    /// </summary>
    /// <param name="a">Start color.</param>
    /// <param name="b">End color.</param>
    /// <param name="step">Current terrace step.</param>
    /// <returns>Interpolated color.</returns>
    public static Color TerraceColorLerp(Color a, Color b, int step)
    {
        float h = step * HorizontalTerraceStepSize;
        return a.Lerp(b, h);
    }

    #endregion
}
