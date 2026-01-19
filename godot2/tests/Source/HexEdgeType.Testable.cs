/// <summary>
/// Describes the edge connection type between two hex cells based on elevation difference.
/// Testable version - identical to main HexEdgeType.cs
/// </summary>
public enum HexEdgeType
{
    Flat,   // Same elevation
    Slope,  // 1 level difference - terraced
    Cliff   // 2+ level difference - vertical
}
