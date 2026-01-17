namespace HexGame.Core;

/// <summary>
/// Manages the hex grid data structure containing all cells.
/// </summary>
public class HexGrid
{
    /// <summary>
    /// Width of the grid in cells.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Height of the grid in cells.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Dictionary storing all cells keyed by their coordinates.
    /// </summary>
    private readonly Dictionary<Vector2I, HexCell> _cells = new();

    /// <summary>
    /// Creates a new hex grid with the specified dimensions.
    /// </summary>
    /// <param name="width">Grid width in cells.</param>
    /// <param name="height">Grid height in cells.</param>
    public HexGrid(int width = 64, int height = 64)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Initializes the grid with empty cells.
    /// </summary>
    public void Initialize()
    {
        _cells.Clear();
        for (int r = 0; r < Height; r++)
        {
            for (int q = 0; q < Width; q++)
            {
                var cell = new HexCell
                {
                    Q = q,
                    R = r,
                    Elevation = 0,
                    TerrainType = TerrainType.Plains
                };
                _cells[new Vector2I(q, r)] = cell;
            }
        }
    }

    /// <summary>
    /// Gets the cell at the specified coordinates.
    /// </summary>
    /// <param name="q">Q coordinate.</param>
    /// <param name="r">R coordinate.</param>
    /// <returns>The cell, or null if coordinates are invalid.</returns>
    public HexCell? GetCell(int q, int r)
    {
        var key = new Vector2I(q, r);
        return _cells.TryGetValue(key, out var cell) ? cell : null;
    }

    /// <summary>
    /// Gets the cell at the specified coordinates.
    /// </summary>
    /// <param name="coordinates">Hex coordinates.</param>
    /// <returns>The cell, or null if coordinates are invalid.</returns>
    public HexCell? GetCell(HexCoordinates coordinates)
    {
        return GetCell(coordinates.Q, coordinates.R);
    }

    /// <summary>
    /// Sets a cell at the specified coordinates.
    /// </summary>
    /// <param name="q">Q coordinate.</param>
    /// <param name="r">R coordinate.</param>
    /// <param name="cell">The cell to set.</param>
    public void SetCell(int q, int r, HexCell cell)
    {
        _cells[new Vector2I(q, r)] = cell;
    }

    /// <summary>
    /// Gets the neighbor of a cell in the specified direction.
    /// </summary>
    /// <param name="cell">The source cell.</param>
    /// <param name="direction">The direction.</param>
    /// <returns>The neighbor cell, or null if out of bounds.</returns>
    public HexCell? GetNeighbor(HexCell cell, HexDirection direction)
    {
        var offset = direction.GetOffset();
        return GetCell(cell.Q + offset.X, cell.R + offset.Y);
    }

    /// <summary>
    /// Gets all valid neighbors of a cell.
    /// </summary>
    /// <param name="cell">The source cell.</param>
    /// <returns>List of valid neighbor cells.</returns>
    public List<HexCell> GetNeighbors(HexCell cell)
    {
        var neighbors = new List<HexCell>(6);
        for (int dir = 0; dir < 6; dir++)
        {
            var neighbor = GetNeighbor(cell, (HexDirection)dir);
            if (neighbor != null)
            {
                neighbors.Add(neighbor);
            }
        }
        return neighbors;
    }

    /// <summary>
    /// Checks if coordinates are within the grid bounds.
    /// </summary>
    /// <param name="q">Q coordinate.</param>
    /// <param name="r">R coordinate.</param>
    /// <returns>True if coordinates are valid.</returns>
    public bool IsValid(int q, int r)
    {
        return q >= 0 && q < Width && r >= 0 && r < Height;
    }

    /// <summary>
    /// Gets all cells in the grid.
    /// </summary>
    /// <returns>Enumerable of all cells.</returns>
    public IEnumerable<HexCell> GetAllCells()
    {
        return _cells.Values;
    }

    /// <summary>
    /// Gets the total number of cells in the grid.
    /// </summary>
    public int CellCount => _cells.Count;

    /// <summary>
    /// Clears all cells from the grid.
    /// </summary>
    public void Clear()
    {
        _cells.Clear();
    }

    /// <summary>
    /// Resizes the grid to new dimensions and reinitializes.
    /// </summary>
    /// <param name="width">New width.</param>
    /// <param name="height">New height.</param>
    public void Resize(int width, int height)
    {
        Width = width;
        Height = height;
        Initialize();
    }

    /// <summary>
    /// Gets cells within a certain hex distance of a center cell.
    /// </summary>
    /// <param name="center">Center cell.</param>
    /// <param name="radius">Maximum distance in hex steps.</param>
    /// <returns>Cells within the radius.</returns>
    public IEnumerable<HexCell> GetCellsInRadius(HexCell center, int radius)
    {
        var centerCoords = center.Coordinates;
        foreach (var cell in _cells.Values)
        {
            if (centerCoords.DistanceTo(cell.Coordinates) <= radius)
            {
                yield return cell;
            }
        }
    }
}
