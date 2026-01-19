using Godot;

/// <summary>
/// Defines hexagon geometry constants.
/// Ported exactly from Catlike Coding Hex Map Tutorial 1.
/// </summary>
public static class HexMetrics
{
    public const float OuterRadius = 10f;

    public const float InnerRadius = OuterRadius * 0.866025404f;

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
}
