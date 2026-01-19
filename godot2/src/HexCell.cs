using Godot;

/// <summary>
/// Represents a single hexagonal cell.
/// Ported exactly from Catlike Coding Hex Map Tutorial 1.
/// </summary>
public partial class HexCell : Node3D
{
    public HexCoordinates Coordinates;
    public Color Color;
}
