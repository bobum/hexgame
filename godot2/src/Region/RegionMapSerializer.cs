using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
#if !TEST
using Godot;
#endif

namespace HexGame.Region;

/// <summary>
/// Handles serialization and deserialization of RegionMap world data.
/// Uses JSON format for human readability and easy debugging.
/// </summary>
public class RegionMapSerializer
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Saves a region map to the specified file path.
    /// </summary>
    public async Task<bool> SaveAsync(RegionMap map, string path, CancellationToken ct = default)
    {
        try
        {
            // Use forward-slash split for Godot paths, Path.GetDirectoryName for system paths
            string? dir;
            if (path.StartsWith("user://") || path.StartsWith("res://"))
            {
                var lastSlash = path.LastIndexOf('/');
                dir = lastSlash > 0 ? path.Substring(0, lastSlash) : null;
            }
            else
            {
                dir = Path.GetDirectoryName(path);
            }

            if (!string.IsNullOrEmpty(dir))
            {
                EnsureDirectoryExists(dir);
            }

            ct.ThrowIfCancellationRequested();

            var json = JsonSerializer.Serialize(map, _jsonOptions);
            await WriteFileAsync(path, json, ct);

#if !TEST
            GD.Print($"[RegionMapSerializer] Saved world '{map.WorldName}' with {map.Regions.Count} regions to {path}");
#endif
            return true;
        }
        catch (OperationCanceledException)
        {
#if !TEST
            GD.Print("[RegionMapSerializer] Save cancelled");
#endif
            return false;
        }
        catch (Exception ex)
        {
#if !TEST
            GD.PrintErr($"[RegionMapSerializer] Save failed: {ex.Message}");
#else
            _ = ex;
#endif
            return false;
        }
    }

    /// <summary>
    /// Loads a region map from the specified file path.
    /// </summary>
    public async Task<RegionMap?> LoadAsync(string path, CancellationToken ct = default)
    {
        try
        {
            var json = await ReadFileAsync(path, ct);
            if (json == null)
            {
#if !TEST
                GD.PrintErr($"[RegionMapSerializer] Failed to read file: {path}");
#endif
                return null;
            }

            ct.ThrowIfCancellationRequested();

            var map = JsonSerializer.Deserialize<RegionMap>(json, _jsonOptions);

#if !TEST
            GD.Print($"[RegionMapSerializer] Loaded world '{map?.WorldName}' with {map?.Regions.Count} regions from {path}");
#endif
            return map;
        }
        catch (OperationCanceledException)
        {
#if !TEST
            GD.Print("[RegionMapSerializer] Load cancelled");
#endif
            return null;
        }
        catch (Exception ex)
        {
#if !TEST
            GD.PrintErr($"[RegionMapSerializer] Load failed: {ex.Message}");
#else
            _ = ex;
#endif
            return null;
        }
    }

    /// <summary>
    /// Synchronous save for convenience.
    /// </summary>
    public bool Save(RegionMap map, string path)
    {
        return SaveAsync(map, path).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Synchronous load for convenience.
    /// </summary>
    public RegionMap? Load(string path)
    {
        return LoadAsync(path).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the default world map file path.
    /// </summary>
    public static string GetDefaultWorldPath(string worldName)
    {
        var sanitized = worldName.ToLowerInvariant().Replace(" ", "_");
        return Path.Combine(RegionConfig.RegionsDirectory, $"{sanitized}.world");
    }

    #region File I/O

    private async Task WriteFileAsync(string path, string content, CancellationToken ct)
    {
#if !TEST
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
                file.StoreString(content);
            }, ct);
        }
        else
#endif
        {
            await File.WriteAllTextAsync(path, content, Encoding.UTF8, ct);
        }
    }

    private async Task<string?> ReadFileAsync(string path, CancellationToken ct)
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
                return file.GetAsText();
            }, ct);
        }
        else
#endif
        {
            if (!File.Exists(path))
                return null;
            return await File.ReadAllTextAsync(path, Encoding.UTF8, ct);
        }
    }

    private void EnsureDirectoryExists(string path)
    {
#if !TEST
        if (path.StartsWith("user://"))
        {
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
}
