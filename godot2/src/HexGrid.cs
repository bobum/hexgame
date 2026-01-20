using Godot;

/// <summary>
/// Creates and manages a hexagonal grid using chunks.
/// Ported from Catlike Coding Hex Map Tutorial 5.
/// </summary>
public partial class HexGrid : Node3D
{
    [Export] public int ChunkCountX = 4;
    [Export] public int ChunkCountZ = 3;
    [Export] public PackedScene? CellPrefab;
    [Export] public PackedScene? CellLabelPrefab;
    [Export] public Texture2D? NoiseSource;
    [Export] public Material? HexMaterial;
    [Export] public Color DefaultColor = Colors.White;
    [Export] public Color TouchedColor = Colors.Magenta;

    private int _cellCountX;
    private int _cellCountZ;
    private HexCell[] _cells = null!;
    private HexGridChunk[] _chunks = null!;

    // Colors from Catlike Coding tutorial palette
    private static readonly Color[] _colors = {
        new Color(1f, 1f, 0f),      // Yellow
        new Color(0f, 1f, 0f),      // Green
        new Color(0f, 0f, 1f),      // Blue
        new Color(1f, 1f, 1f),      // White
    };

    public override void _Ready()
    {
        // Initialize noise texture for perturbation
        InitializeNoiseSource();

        _cellCountX = ChunkCountX * HexMetrics.ChunkSizeX;
        _cellCountZ = ChunkCountZ * HexMetrics.ChunkSizeZ;

        CreateChunks();
        CreateCells();
    }

    private void CreateChunks()
    {
        _chunks = new HexGridChunk[ChunkCountX * ChunkCountZ];

        for (int z = 0, i = 0; z < ChunkCountZ; z++)
        {
            for (int x = 0; x < ChunkCountX; x++)
            {
                HexGridChunk chunk = new HexGridChunk();
                chunk.Name = $"Chunk_{x}_{z}";
                _chunks[i++] = chunk;
                AddChild(chunk);
                chunk.Initialize(HexMaterial);
            }
        }
    }

    private void CreateCells()
    {
        _cells = new HexCell[_cellCountZ * _cellCountX];

        for (int z = 0, i = 0; z < _cellCountZ; z++)
        {
            for (int x = 0; x < _cellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position.X = (x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f);
        position.Y = 0f;
        position.Z = z * (HexMetrics.OuterRadius * 1.5f);

        HexCell cell = CellPrefab!.Instantiate<HexCell>();
        _cells[i] = cell;
        cell.Position = position;
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

        // Establish neighbor connections
        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, _cells[i - 1]);
        }
        if (z > 0)
        {
            if ((z & 1) == 0) // Even row
            {
                cell.SetNeighbor(HexDirection.SE, _cells[i - _cellCountX]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, _cells[i - _cellCountX - 1]);
                }
            }
            else // Odd row
            {
                cell.SetNeighbor(HexDirection.SW, _cells[i - _cellCountX]);
                if (x < _cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, _cells[i - _cellCountX + 1]);
                }
            }
        }

        // Create coordinate label
        Label3D? label = null;
        if (CellLabelPrefab != null)
        {
            label = CellLabelPrefab.Instantiate<Label3D>();
            label.Position = new Vector3(position.X, 0.1f, position.Z);
            label.Text = cell.Coordinates.ToStringOnSeparateLines();
            label.RotationDegrees = new Vector3(-90, 0, 0);
            cell.UiLabel = label;
        }

        AddCellToChunk(x, z, cell);

        // Set initial values AFTER adding to chunk so Refresh works
        // Color by chunk to visualize chunk boundaries
        int chunkX = x / HexMetrics.ChunkSizeX;
        int chunkZ = z / HexMetrics.ChunkSizeZ;
        int chunkIndex = chunkX + chunkZ * ChunkCountX;
        cell.Color = _colors[chunkIndex % _colors.Length];
        cell.Elevation = (x + z) % 4;
    }

    private void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.ChunkSizeX;
        int chunkZ = z / HexMetrics.ChunkSizeZ;
        HexGridChunk chunk = _chunks[chunkX + chunkZ * ChunkCountX];

        int localX = x - chunkX * HexMetrics.ChunkSizeX;
        int localZ = z - chunkZ * HexMetrics.ChunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.ChunkSizeX, cell);
    }

    public HexCell? GetCell(Vector3 position)
    {
        position = ToLocal(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        return GetCell(coordinates);
    }

    public HexCell? GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;
        if (z < 0 || z >= _cellCountZ)
        {
            return null;
        }
        int x = coordinates.X + z / 2;
        if (x < 0 || x >= _cellCountX)
        {
            return null;
        }
        return _cells[x + z * _cellCountX];
    }

    public void ShowUI(bool visible)
    {
        for (int i = 0; i < _chunks.Length; i++)
        {
            _chunks[i].ShowUI(visible);
        }
    }

    private void InitializeNoiseSource()
    {
        if (NoiseSource != null)
        {
            HexMetrics.NoiseSource = NoiseSource.GetImage();
            GD.Print($"Noise texture from export: {HexMetrics.NoiseSource.GetWidth()}x{HexMetrics.NoiseSource.GetHeight()}");
        }
        else
        {
            // Load noise texture (same texture used in Catlike Coding tutorial)
            var texture = GD.Load<Texture2D>("res://assets/noise.png");
            if (texture != null)
            {
                HexMetrics.NoiseSource = texture.GetImage();
                GD.Print($"Noise texture loaded: {HexMetrics.NoiseSource.GetWidth()}x{HexMetrics.NoiseSource.GetHeight()}");
            }
            else
            {
                GD.PrintErr("CRITICAL: Failed to load noise texture - perturbation will NOT work!");
            }
        }

        // Verify noise is working
        if (HexMetrics.NoiseSource == null)
        {
            GD.PrintErr("CRITICAL: NoiseSource is null after initialization!");
        }
    }
}
