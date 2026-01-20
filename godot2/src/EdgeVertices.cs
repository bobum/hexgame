using Godot;

/// <summary>
/// Represents a subdivided edge with 5 vertices.
/// Ported from Catlike Coding Hex Map Tutorial 6.
/// Changed from 4 vertices (Tutorial 4) to 5 vertices for river support.
/// </summary>
public struct EdgeVertices
{
    public Vector3 V1, V2, V3, V4, V5;

    /// <summary>
    /// Creates edge vertices at 0%, 25%, 50%, 75%, 100% positions.
    /// </summary>
    public EdgeVertices(Vector3 corner1, Vector3 corner2)
    {
        V1 = corner1;
        V2 = corner1.Lerp(corner2, 0.25f);
        V3 = corner1.Lerp(corner2, 0.5f);
        V4 = corner1.Lerp(corner2, 0.75f);
        V5 = corner2;
    }

    /// <summary>
    /// Creates edge vertices with custom outer step for river channels.
    /// V2 and V4 are positioned at outerStep from edges, creating narrower channel.
    /// Used with outerStep = 1/6 for river triangulation.
    /// </summary>
    public EdgeVertices(Vector3 corner1, Vector3 corner2, float outerStep)
    {
        V1 = corner1;
        V2 = corner1.Lerp(corner2, outerStep);
        V3 = corner1.Lerp(corner2, 0.5f);
        V4 = corner1.Lerp(corner2, 1f - outerStep);
        V5 = corner2;
    }

    public static EdgeVertices TerraceLerp(EdgeVertices a, EdgeVertices b, int step)
    {
        EdgeVertices result;
        result.V1 = HexMetrics.TerraceLerp(a.V1, b.V1, step);
        result.V2 = HexMetrics.TerraceLerp(a.V2, b.V2, step);
        result.V3 = HexMetrics.TerraceLerp(a.V3, b.V3, step);
        result.V4 = HexMetrics.TerraceLerp(a.V4, b.V4, step);
        result.V5 = HexMetrics.TerraceLerp(a.V5, b.V5, step);
        return result;
    }
}
