using Godot;

/// <summary>
/// Represents a single hexagonal cell.
/// Ported exactly from Catlike Coding Hex Map Tutorials 1-2.
/// </summary>
public partial class HexCell : Node3D
{
    public HexCoordinates Coordinates;
    public Color Color;

    private HexCell[] _neighbors = new HexCell[6];

    public HexCell GetNeighbor(HexDirection direction)
    {
        return _neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        _neighbors[(int)direction] = cell;
        cell._neighbors[(int)direction.Opposite()] = this;
    }
}
