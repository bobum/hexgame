/// <summary>
/// Describes the edge connection type between two hex cells based on elevation difference.
/// Ported exactly from Catlike Coding Hex Map Tutorial 3.
/// </summary>
public enum HexEdgeType
{
    Flat,   // Same elevation
    Slope,  // 1 level difference - terraced
    Cliff   // 2+ level difference - vertical
}
