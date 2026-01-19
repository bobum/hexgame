using Godot;

/// <summary>
/// Creates and manages a hexagonal grid.
/// Ported exactly from Catlike Coding Hex Map Tutorials 1-2.
/// </summary>
public partial class HexGrid : Node3D
{
    [Export] public int Width = 6;
    [Export] public int Height = 6;
    [Export] public PackedScene? CellPrefab;
    [Export] public PackedScene? CellLabelPrefab;
    [Export] public Color DefaultColor = Colors.White;
    [Export] public Color TouchedColor = Colors.Magenta;

    private HexCell[] _cells = null!;
    private HexMesh _hexMesh = null!;

    // Colors from Catlike Coding tutorial palette
    private static readonly Color[] _colors = {
        new Color(1f, 1f, 0f),      // Yellow
        new Color(0f, 1f, 0f),      // Green
        new Color(0f, 0f, 1f),      // Blue
        new Color(1f, 1f, 1f),      // White
    };

    public override void _Ready()
    {
        _hexMesh = GetNode<HexMesh>("HexMesh");
        _cells = new HexCell[Height * Width];

        for (int z = 0, i = 0; z < Height; z++)
        {
            for (int x = 0; x < Width; x++)
            {
                CreateCell(x, z, i++);
            }
        }

        _hexMesh.Triangulate(_cells);
    }

    private void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position.X = (x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f);
        position.Y = 0f;
        position.Z = z * (HexMetrics.OuterRadius * 1.5f);

        HexCell cell = CellPrefab!.Instantiate<HexCell>();
        _cells[i] = cell;
        AddChild(cell);
        cell.Position = position;
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.Color = _colors[(x + z) % _colors.Length];

        // Establish neighbor connections
        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, _cells[i - 1]);
        }
        if (z > 0)
        {
            if ((z & 1) == 0) // Even row
            {
                cell.SetNeighbor(HexDirection.SE, _cells[i - Width]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, _cells[i - Width - 1]);
                }
            }
            else // Odd row
            {
                cell.SetNeighbor(HexDirection.SW, _cells[i - Width]);
                if (x < Width - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, _cells[i - Width + 1]);
                }
            }
        }

        // Create coordinate label
        if (CellLabelPrefab != null)
        {
            var label = CellLabelPrefab.Instantiate<Label3D>();
            AddChild(label);
            label.Position = new Vector3(position.X, 0.5f, position.Z);
            label.Text = cell.Coordinates.ToStringOnSeparateLines();
            label.RotationDegrees = new Vector3(-90, 0, 0);
        }
    }

    public void ColorCell(Vector3 position, Color color)
    {
        position = ToLocal(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * Width + coordinates.Z / 2;
        if (index >= 0 && index < _cells.Length)
        {
            HexCell cell = _cells[index];
            cell.Color = color;
            _hexMesh.Triangulate(_cells);
        }
    }

    public HexCell? GetCell(Vector3 position)
    {
        position = ToLocal(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * Width + coordinates.Z / 2;
        if (index >= 0 && index < _cells.Length)
        {
            return _cells[index];
        }
        return null;
    }
}
