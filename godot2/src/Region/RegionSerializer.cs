using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
#if !TEST
using Godot;
#endif
using HexGame.Generation;

namespace HexGame.Region;

/// <summary>
/// Handles binary serialization and deserialization of region data.
/// Uses a compact binary format for efficient storage (~650KB per 200x200 region).
///
/// File Format:
/// [Header: 32 bytes]
///   - Magic: 4 bytes (0x47524858 "HXRG")
///   - Version: 4 bytes
///   - RegionId: 16 bytes (GUID)
///   - Width: 4 bytes
///   - Height: 4 bytes
/// [Metadata: Variable]
///   - NameLength: 4 bytes
///   - Name: NameLength bytes (UTF-8)
///   - Seed: 4 bytes
///   - GeneratedAt: 8 bytes (ticks)
///   - ConnectionCount: 4 bytes
///   - Connections: ConnectionCount * ConnectionRecord
/// [Cell Data: Width * Height * 16 bytes]
///   - PackedCellData records
/// </summary>
public class RegionSerializer
{
    /// <summary>
    /// Fired during save operations with stage name and progress (0.0-1.0).
    /// </summary>
    public event Action<string, float>? SaveProgress;

    /// <summary>
    /// Fired during load operations with stage name and progress (0.0-1.0).
    /// </summary>
    public event Action<string, float>? LoadProgress;

    /// <summary>
    /// Saves a region to the specified file path.
    /// </summary>
    public async Task<bool> SaveAsync(RegionData region, string path, CancellationToken ct = default)
    {
        try
        {
            ReportSaveProgress("Preparing", 0f);

            // Ensure directory exists
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                EnsureDirectoryExists(dir);
            }

            ct.ThrowIfCancellationRequested();

            // Serialize to memory first
            ReportSaveProgress("Serializing", 0.1f);
            var data = await Task.Run(() => SerializeToBytes(region, ct), ct);

            ct.ThrowIfCancellationRequested();

            // Write to file
            ReportSaveProgress("Writing file", 0.8f);
            await WriteFileAsync(path, data, ct);

            ReportSaveProgress("Complete", 1.0f);
#if !TEST
            GD.Print($"[RegionSerializer] Saved region '{region.Name}' ({region.Width}x{region.Height}) to {path} ({data.Length} bytes)");
#endif
            return true;
        }
        catch (OperationCanceledException)
        {
#if !TEST
            GD.Print("[RegionSerializer] Save cancelled");
#endif
            return false;
        }
        catch (Exception ex)
        {
#if !TEST
            GD.PrintErr($"[RegionSerializer] Save failed: {ex.Message}");
#else
            _ = ex;
#endif
            return false;
        }
    }

    /// <summary>
    /// Loads a region from the specified file path.
    /// </summary>
    public async Task<RegionData?> LoadAsync(string path, CancellationToken ct = default)
    {
        try
        {
            ReportLoadProgress("Reading file", 0f);

            // Read file
            var data = await ReadFileAsync(path, ct);
            if (data == null)
            {
#if !TEST
                GD.PrintErr($"[RegionSerializer] Failed to read file: {path}");
#endif
                return null;
            }

            ct.ThrowIfCancellationRequested();

            // Deserialize
            ReportLoadProgress("Deserializing", 0.2f);
            var region = await Task.Run(() => DeserializeFromBytes(data, ct), ct);

            ReportLoadProgress("Complete", 1.0f);
#if !TEST
            GD.Print($"[RegionSerializer] Loaded region '{region?.Name}' ({region?.Width}x{region?.Height}) from {path}");
#endif
            return region;
        }
        catch (OperationCanceledException)
        {
#if !TEST
            GD.Print("[RegionSerializer] Load cancelled");
#endif
            return null;
        }
        catch (Exception ex)
        {
#if !TEST
            GD.PrintErr($"[RegionSerializer] Load failed: {ex.Message}");
#else
            _ = ex;
#endif
            return null;
        }
    }

    /// <summary>
    /// Loads only the metadata (header) from a region file.
    /// Much faster than full load - useful for Region Map display.
    /// </summary>
    public async Task<RegionMetadata?> LoadMetadataAsync(string path, CancellationToken ct = default)
    {
        try
        {
            var data = await ReadFileAsync(path, ct);
            if (data == null) return null;

            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            // Read and validate header
            var magic = reader.ReadUInt32();
            if (magic != RegionConfig.FileMagicNumber)
            {
#if !TEST
                GD.PrintErr($"[RegionSerializer] Invalid magic number in {path}");
#endif
                return null;
            }

            var version = reader.ReadUInt32();
            if (version > RegionConfig.FileVersion)
            {
#if !TEST
                GD.PrintErr($"[RegionSerializer] File version {version} is newer than supported {RegionConfig.FileVersion}");
#endif
                return null;
            }

            var metadata = new RegionMetadata
            {
                RegionId = new Guid(reader.ReadBytes(16)),
                Width = reader.ReadInt32(),
                Height = reader.ReadInt32()
            };

            // Read metadata section
            var nameLength = reader.ReadInt32();
            metadata.Name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
            metadata.Seed = reader.ReadInt32();
            metadata.GeneratedAt = new DateTime(reader.ReadInt64(), DateTimeKind.Utc);

            // Read connections
            var connectionCount = reader.ReadInt32();
            for (int i = 0; i < connectionCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                metadata.Connections.Add(ReadConnection(reader));
            }

            return metadata;
        }
        catch (Exception ex)
        {
#if !TEST
            GD.PrintErr($"[RegionSerializer] LoadMetadata failed: {ex.Message}");
#else
            _ = ex; // Suppress unused variable warning in test mode
#endif
            return null;
        }
    }

    /// <summary>
    /// Synchronous save for testing purposes.
    /// </summary>
    public bool Save(RegionData region, string path)
    {
        return SaveAsync(region, path).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Synchronous load for testing purposes.
    /// </summary>
    public RegionData? Load(string path)
    {
        return LoadAsync(path).GetAwaiter().GetResult();
    }

    #region Serialization

    private byte[] SerializeToBytes(RegionData region, CancellationToken ct)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8);

        // Header
        writer.Write(RegionConfig.FileMagicNumber);
        writer.Write(RegionConfig.FileVersion);
        writer.Write(region.RegionId.ToByteArray());
        writer.Write(region.Width);
        writer.Write(region.Height);

        // Metadata
        var nameBytes = Encoding.UTF8.GetBytes(region.Name);
        writer.Write(nameBytes.Length);
        writer.Write(nameBytes);
        writer.Write(region.Seed);
        writer.Write(region.GeneratedAt.Ticks);

        // Connections
        writer.Write(region.Connections.Count);
        foreach (var conn in region.Connections)
        {
            ct.ThrowIfCancellationRequested();
            WriteConnection(writer, conn);
        }

        // Cell data
        int totalCells = region.Cells.Length;
        int cellsWritten = 0;
        foreach (var cell in region.Cells)
        {
            ct.ThrowIfCancellationRequested();

            var packed = PackedCellData.Pack(in cell);
            WritePackedCell(writer, packed);

            cellsWritten++;
            if (cellsWritten % 10000 == 0)
            {
                float progress = 0.1f + 0.7f * ((float)cellsWritten / totalCells);
                ReportSaveProgress("Serializing cells", progress);
            }
        }

        return stream.ToArray();
    }

    private RegionData? DeserializeFromBytes(byte[] data, CancellationToken ct)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream, Encoding.UTF8);

        // Header
        var magic = reader.ReadUInt32();
        if (magic != RegionConfig.FileMagicNumber)
        {
#if !TEST
            GD.PrintErr("[RegionSerializer] Invalid magic number");
#endif
            return null;
        }

        var version = reader.ReadUInt32();
        if (version > RegionConfig.FileVersion)
        {
#if !TEST
            GD.PrintErr($"[RegionSerializer] Unsupported version {version}");
#endif
            return null;
        }

        var region = new RegionData
        {
            RegionId = new Guid(reader.ReadBytes(16)),
            Width = reader.ReadInt32(),
            Height = reader.ReadInt32()
        };

        // Metadata
        var nameLength = reader.ReadInt32();
        region.Name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
        region.Seed = reader.ReadInt32();
        region.GeneratedAt = new DateTime(reader.ReadInt64(), DateTimeKind.Utc);

        // Connections
        var connectionCount = reader.ReadInt32();
        for (int i = 0; i < connectionCount; i++)
        {
            ct.ThrowIfCancellationRequested();
            region.Connections.Add(ReadConnection(reader));
        }

        // Cell data
        int totalCells = region.Width * region.Height;
        region.Cells = new CellData[totalCells];

        for (int i = 0; i < totalCells; i++)
        {
            ct.ThrowIfCancellationRequested();

            var packed = ReadPackedCell(reader);
            region.Cells[i] = packed.Unpack();

            if (i % 10000 == 0)
            {
                float progress = 0.2f + 0.8f * ((float)i / totalCells);
                ReportLoadProgress("Loading cells", progress);
            }
        }

        return region;
    }

    private void WriteConnection(BinaryWriter writer, RegionConnection conn)
    {
        writer.Write(conn.TargetRegionId.ToByteArray());
        var nameBytes = Encoding.UTF8.GetBytes(conn.TargetRegionName);
        writer.Write(nameBytes.Length);
        writer.Write(nameBytes);
        writer.Write(conn.DeparturePortIndex);
        writer.Write(conn.ArrivalPortIndex);
        writer.Write(conn.TravelTimeMinutes);
        writer.Write(conn.DangerLevel);
    }

    private RegionConnection ReadConnection(BinaryReader reader)
    {
        return new RegionConnection
        {
            TargetRegionId = new Guid(reader.ReadBytes(16)),
            TargetRegionName = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32())),
            DeparturePortIndex = reader.ReadInt32(),
            ArrivalPortIndex = reader.ReadInt32(),
            TravelTimeMinutes = reader.ReadSingle(),
            DangerLevel = reader.ReadSingle()
        };
    }

    private void WritePackedCell(BinaryWriter writer, PackedCellData packed)
    {
        writer.Write(packed.X);
        writer.Write(packed.Z);
        writer.Write(packed.Elevation);
        writer.Write(packed.WaterLevel);
        writer.Write(packed.TerrainTypeIndex);
        writer.Write(packed.SpecialIndex);
        writer.Write(packed.FeatureFlags);
        writer.Write(packed.RiverFlags);
        writer.Write(packed.RoadFlags);
        writer.Write(packed.Reserved);
        writer.Write(packed.MoisturePacked);
        writer.Write(packed.Padding);
    }

    private PackedCellData ReadPackedCell(BinaryReader reader)
    {
        return new PackedCellData
        {
            X = reader.ReadInt16(),
            Z = reader.ReadInt16(),
            Elevation = reader.ReadSByte(),
            WaterLevel = reader.ReadSByte(),
            TerrainTypeIndex = reader.ReadByte(),
            SpecialIndex = reader.ReadByte(),
            FeatureFlags = reader.ReadByte(),
            RiverFlags = reader.ReadByte(),
            RoadFlags = reader.ReadByte(),
            Reserved = reader.ReadByte(),
            MoisturePacked = reader.ReadUInt16(),
            Padding = reader.ReadUInt16()
        };
    }

    #endregion

    #region File I/O

    private async Task WriteFileAsync(string path, byte[] data, CancellationToken ct)
    {
#if !TEST
        // Use Godot's FileAccess for user:// paths, System.IO for absolute paths
        if (path.StartsWith("user://") || path.StartsWith("res://"))
        {
            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
                if (file == null)
                {
                    throw new IOException($"Failed to open file for writing: {Godot.FileAccess.GetOpenError()}");
                }
                file.StoreBuffer(data);
            }, ct);
        }
        else
#endif
        {
            await File.WriteAllBytesAsync(path, data, ct);
        }
    }

    private async Task<byte[]?> ReadFileAsync(string path, CancellationToken ct)
    {
#if !TEST
        if (path.StartsWith("user://") || path.StartsWith("res://"))
        {
            return await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    return null;
                }
                return file.GetBuffer((long)file.GetLength());
            }, ct);
        }
        else
#endif
        {
            if (!File.Exists(path))
                return null;
            return await File.ReadAllBytesAsync(path, ct);
        }
    }

    private void EnsureDirectoryExists(string path)
    {
#if !TEST
        if (path.StartsWith("user://"))
        {
            // For Godot paths, use DirAccess
            var dir = Godot.DirAccess.Open("user://");
            if (dir != null)
            {
                var relativePath = path.Replace("user://", "");
                dir.MakeDirRecursive(relativePath);
            }
        }
        else
#endif
        {
            Directory.CreateDirectory(path);
        }
    }

    #endregion

    #region Progress Reporting

    private void ReportSaveProgress(string stage, float progress)
    {
        SaveProgress?.Invoke(stage, progress);
    }

    private void ReportLoadProgress(string stage, float progress)
    {
        LoadProgress?.Invoke(stage, progress);
    }

    #endregion
}
