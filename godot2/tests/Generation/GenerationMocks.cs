using System.Collections.Generic;

namespace HexMapTutorial.Tests.Generation;

/// <summary>
/// Mock cell for testing map generation without Godot dependencies.
/// Mimics HexCell properties used during procedural generation.
/// </summary>
public class MockGenerationCell
{
    public HexCoordinates Coordinates { get; set; }
    public int Elevation { get; set; }
    public int WaterLevel { get; set; }
    public int TerrainTypeIndex { get; set; }
    public int UrbanLevel { get; set; }
    public int FarmLevel { get; set; }
    public int PlantLevel { get; set; }
    public int SpecialIndex { get; set; }
    public bool Walled { get; set; }

    // River state
    public bool HasIncomingRiver { get; set; }
    public bool HasOutgoingRiver { get; set; }
    public HexDirection IncomingRiver { get; set; }
    public HexDirection OutgoingRiver { get; set; }

    // Road state
    public bool[] Roads { get; } = new bool[6];

    // Neighbors
    public MockGenerationCell?[] Neighbors { get; } = new MockGenerationCell?[6];

    public bool IsUnderwater => WaterLevel > Elevation;

    public MockGenerationCell(int x, int z)
    {
        Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
    }

    public void RemoveRiver()
    {
        HasIncomingRiver = false;
        HasOutgoingRiver = false;
    }

    public void RemoveRoads()
    {
        for (int i = 0; i < Roads.Length; i++)
            Roads[i] = false;
    }

    public MockGenerationCell? GetNeighbor(HexDirection direction) => Neighbors[(int)direction];

    public void SetNeighbor(HexDirection direction, MockGenerationCell neighbor)
    {
        Neighbors[(int)direction] = neighbor;
        neighbor.Neighbors[(int)direction.Opposite()] = this;
    }
}

/// <summary>
/// Mock grid for testing map generation without Godot dependencies.
/// </summary>
public class MockGenerationGrid
{
    private readonly MockGenerationCell[,] _cells;

    public int CellCountX { get; }
    public int CellCountZ { get; }
    public int CellCount => CellCountX * CellCountZ;

    public MockGenerationGrid(int cellCountX, int cellCountZ)
    {
        CellCountX = cellCountX;
        CellCountZ = cellCountZ;
        _cells = new MockGenerationCell[cellCountX, cellCountZ];

        // Create cells and establish neighbor connections
        for (int z = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                var cell = new MockGenerationCell(x, z);
                _cells[x, z] = cell;

                // Connect to western neighbor
                if (x > 0)
                    cell.SetNeighbor(HexDirection.W, _cells[x - 1, z]);

                // Connect to southern neighbors (following HexGrid pattern)
                if (z > 0)
                {
                    if ((z & 1) == 0) // Even row
                    {
                        cell.SetNeighbor(HexDirection.SE, _cells[x, z - 1]);
                        if (x > 0)
                            cell.SetNeighbor(HexDirection.SW, _cells[x - 1, z - 1]);
                    }
                    else // Odd row
                    {
                        cell.SetNeighbor(HexDirection.SW, _cells[x, z - 1]);
                        if (x < cellCountX - 1)
                            cell.SetNeighbor(HexDirection.SE, _cells[x + 1, z - 1]);
                    }
                }
            }
        }
    }

    public MockGenerationCell? GetCellByOffset(int x, int z)
    {
        if (x < 0 || x >= CellCountX || z < 0 || z >= CellCountZ)
            return null;
        return _cells[x, z];
    }

    public IEnumerable<MockGenerationCell> GetAllCells()
    {
        for (int z = 0; z < CellCountZ; z++)
        {
            for (int x = 0; x < CellCountX; x++)
            {
                yield return _cells[x, z];
            }
        }
    }
}
