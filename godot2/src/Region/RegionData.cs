using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HexGame.Generation;

namespace HexGame.Region;

/// <summary>
/// Complete region data including cells and metadata.
/// This is what gets serialized to/from .region files.
/// </summary>
public class RegionData
{
    /// <summary>
    /// Unique identifier for this region.
    /// </summary>
    public Guid RegionId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display name for this region (e.g., "Nassau", "Jamaica").
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Width of the region in cells.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Height of the region in cells.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Seed used to generate this region (for reproducibility).
    /// </summary>
    public int Seed { get; set; }

    /// <summary>
    /// When this region was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// All cell data for this region.
    /// Length should be Width * Height.
    /// </summary>
    public CellData[] Cells { get; set; } = Array.Empty<CellData>();

    /// <summary>
    /// Connections to other regions (ports, travel routes).
    /// </summary>
    public List<RegionConnection> Connections { get; set; } = new();

    /// <summary>
    /// Gets a cell by its local coordinates.
    /// </summary>
    public CellData? GetCell(int x, int z)
    {
        if (x < 0 || x >= Width || z < 0 || z >= Height)
            return null;

        int index = z * Width + x;
        if (index >= Cells.Length)
            return null;

        return Cells[index];
    }

    /// <summary>
    /// Creates an empty region with the specified dimensions.
    /// </summary>
    public static RegionData CreateEmpty(string name, int width, int height, int seed)
    {
        var region = new RegionData
        {
            Name = name,
            Width = width,
            Height = height,
            Seed = seed,
            Cells = new CellData[width * height]
        };

        // Initialize all cells
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                region.Cells[z * width + x] = new CellData(x, z);
            }
        }

        return region;
    }
}

/// <summary>
/// Lightweight metadata for a region (loaded without full cell data).
/// Used for Region Map display and travel planning.
/// </summary>
public class RegionMetadata
{
    public Guid RegionId { get; set; }
    public string Name { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public int Seed { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<RegionConnection> Connections { get; set; } = new();

    /// <summary>
    /// Creates metadata from full region data.
    /// </summary>
    public static RegionMetadata FromRegionData(RegionData region)
    {
        return new RegionMetadata
        {
            RegionId = region.RegionId,
            Name = region.Name,
            Width = region.Width,
            Height = region.Height,
            Seed = region.Seed,
            GeneratedAt = region.GeneratedAt,
            Connections = new List<RegionConnection>(region.Connections)
        };
    }
}

/// <summary>
/// Defines a connection between two regions (travel route).
/// </summary>
public class RegionConnection
{
    /// <summary>
    /// The region this connection leads to.
    /// </summary>
    public Guid TargetRegionId { get; set; }

    /// <summary>
    /// Display name of the target region.
    /// </summary>
    public string TargetRegionName { get; set; } = "";

    /// <summary>
    /// Cell index in this region where departure occurs (port).
    /// </summary>
    public int DeparturePortIndex { get; set; }

    /// <summary>
    /// Cell index in target region where arrival occurs (port).
    /// </summary>
    public int ArrivalPortIndex { get; set; }

    /// <summary>
    /// Base travel time in game-minutes.
    /// </summary>
    public float TravelTimeMinutes { get; set; }

    /// <summary>
    /// Danger level of this route (0.0 = safe, 1.0 = dangerous).
    /// Affects random encounter chances.
    /// </summary>
    public float DangerLevel { get; set; }
}

/// <summary>
/// Packed binary representation of cell data for efficient serialization.
/// Uses 16 bytes per cell vs ~52 bytes for CellData struct.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PackedCellData
{
    // Bytes 0-3: Position
    public short X;           // 2 bytes
    public short Z;           // 2 bytes

    // Bytes 4-7: Terrain
    public sbyte Elevation;   // 1 byte (-128 to 127, plenty for our 0-6 range)
    public sbyte WaterLevel;  // 1 byte
    public byte TerrainTypeIndex;  // 1 byte (0-255)
    public byte SpecialIndex; // 1 byte (0-255)

    // Byte 8: Feature levels packed (2 bits each = 0-3 range)
    // Bits 0-1: UrbanLevel, Bits 2-3: FarmLevel, Bits 4-5: PlantLevel, Bit 6: Walled, Bit 7: Reserved
    public byte FeatureFlags;

    // Byte 9: River flags
    // Bit 0: HasIncomingRiver, Bit 1: HasOutgoingRiver
    // Bits 2-4: IncomingRiverDirection (0-5), Bits 5-7: OutgoingRiverDirection (0-5)
    public byte RiverFlags;

    // Byte 10: Road flags (6 bits for 6 directions)
    // Bits 0-5: Roads in directions NE, E, SE, SW, W, NW
    public byte RoadFlags;

    // Byte 11: Reserved for future use
    public byte Reserved;

    // Bytes 12-15: Moisture as half-precision float (2 bytes) + padding
    public ushort MoisturePacked;  // Half-precision float
    public ushort Padding;         // Alignment padding

    /// <summary>
    /// Packs a CellData struct into the compact binary format.
    /// </summary>
    public static PackedCellData Pack(in CellData cell)
    {
        var packed = new PackedCellData
        {
            X = (short)cell.X,
            Z = (short)cell.Z,
            Elevation = (sbyte)cell.Elevation,
            WaterLevel = (sbyte)cell.WaterLevel,
            TerrainTypeIndex = (byte)cell.TerrainTypeIndex,
            SpecialIndex = (byte)cell.SpecialIndex
        };

        // Pack feature levels (2 bits each) and walled flag
        packed.FeatureFlags = (byte)(
            (cell.UrbanLevel & 0x3) |
            ((cell.FarmLevel & 0x3) << 2) |
            ((cell.PlantLevel & 0x3) << 4) |
            (cell.Walled ? 0x40 : 0)
        );

        // Pack river flags
        packed.RiverFlags = (byte)(
            (cell.HasIncomingRiver ? 0x1 : 0) |
            (cell.HasOutgoingRiver ? 0x2 : 0) |
            ((cell.IncomingRiverDirection & 0x7) << 2) |
            ((cell.OutgoingRiverDirection & 0x7) << 5)
        );

        // Pack road flags
        packed.RoadFlags = (byte)(
            (cell.HasRoadNE ? 0x01 : 0) |
            (cell.HasRoadE ? 0x02 : 0) |
            (cell.HasRoadSE ? 0x04 : 0) |
            (cell.HasRoadSW ? 0x08 : 0) |
            (cell.HasRoadW ? 0x10 : 0) |
            (cell.HasRoadNW ? 0x20 : 0)
        );

        // Pack moisture as half-precision float
        packed.MoisturePacked = FloatToHalf(cell.Moisture);

        return packed;
    }

    /// <summary>
    /// Unpacks the compact binary format back to a CellData struct.
    /// </summary>
    public CellData Unpack()
    {
        var cell = new CellData(X, Z)
        {
            Elevation = Elevation,
            WaterLevel = WaterLevel,
            TerrainTypeIndex = TerrainTypeIndex,
            SpecialIndex = SpecialIndex,

            // Unpack feature levels
            UrbanLevel = FeatureFlags & 0x3,
            FarmLevel = (FeatureFlags >> 2) & 0x3,
            PlantLevel = (FeatureFlags >> 4) & 0x3,
            Walled = (FeatureFlags & 0x40) != 0,

            // Unpack river flags
            HasIncomingRiver = (RiverFlags & 0x1) != 0,
            HasOutgoingRiver = (RiverFlags & 0x2) != 0,
            IncomingRiverDirection = (RiverFlags >> 2) & 0x7,
            OutgoingRiverDirection = (RiverFlags >> 5) & 0x7,

            // Unpack road flags
            HasRoadNE = (RoadFlags & 0x01) != 0,
            HasRoadE = (RoadFlags & 0x02) != 0,
            HasRoadSE = (RoadFlags & 0x04) != 0,
            HasRoadSW = (RoadFlags & 0x08) != 0,
            HasRoadW = (RoadFlags & 0x10) != 0,
            HasRoadNW = (RoadFlags & 0x20) != 0,

            // Unpack moisture
            Moisture = HalfToFloat(MoisturePacked)
        };

        return cell;
    }

    /// <summary>
    /// Converts a float to half-precision (16-bit) representation.
    /// </summary>
    private static ushort FloatToHalf(float value)
    {
        // Use .NET's Half type for accurate conversion
        var half = (Half)value;
        return BitConverter.ToUInt16(BitConverter.GetBytes(half));
    }

    /// <summary>
    /// Converts a half-precision (16-bit) representation to float.
    /// </summary>
    private static float HalfToFloat(ushort value)
    {
        var bytes = BitConverter.GetBytes(value);
        var half = BitConverter.ToHalf(bytes);
        return (float)half;
    }
}
