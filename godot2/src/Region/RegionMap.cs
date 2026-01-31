using System;
using System.Collections.Generic;
using System.Linq;
#if !TEST
using Godot;
#endif

namespace HexGame.Region;

/// <summary>
/// World-level data container for all regions in a game world.
/// Manages region discovery, connections, and current region tracking.
/// </summary>
public class RegionMap
{
    /// <summary>
    /// Display name for this world.
    /// </summary>
    public string WorldName { get; set; } = "New World";

    /// <summary>
    /// Unique identifier for this world save.
    /// </summary>
    public Guid WorldId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The currently active region.
    /// </summary>
    public Guid CurrentRegionId { get; set; }

    /// <summary>
    /// Timestamp when this world was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// All regions in this world.
    /// </summary>
    public List<RegionMapEntry> Regions { get; set; } = new();

    /// <summary>
    /// Gets the currently active region entry.
    /// </summary>
    public RegionMapEntry? GetCurrentRegion()
    {
        return GetRegionById(CurrentRegionId);
    }

    /// <summary>
    /// Gets a region by its ID.
    /// </summary>
    public RegionMapEntry? GetRegionById(Guid id)
    {
        return Regions.FirstOrDefault(r => r.RegionId == id);
    }

    /// <summary>
    /// Gets all regions connected to the specified region.
    /// </summary>
    public IEnumerable<RegionMapEntry> GetConnectedRegions(Guid fromRegionId)
    {
        var fromRegion = GetRegionById(fromRegionId);
        if (fromRegion == null)
            return Enumerable.Empty<RegionMapEntry>();

        return fromRegion.ConnectedRegionIds
            .Select(id => GetRegionById(id))
            .Where(r => r != null)
            .Cast<RegionMapEntry>();
    }

    /// <summary>
    /// Gets all discovered regions.
    /// </summary>
    public IEnumerable<RegionMapEntry> GetDiscoveredRegions()
    {
        return Regions.Where(r => r.IsDiscovered);
    }

    /// <summary>
    /// Adds a new region to the world map.
    /// </summary>
    public void AddRegion(RegionMapEntry entry)
    {
        if (Regions.Any(r => r.RegionId == entry.RegionId))
            return;

        Regions.Add(entry);
    }

    /// <summary>
    /// Creates a bidirectional connection between two regions.
    /// </summary>
    public void ConnectRegions(Guid regionA, Guid regionB, float travelTime = 60f, float dangerLevel = 0f)
    {
        var entryA = GetRegionById(regionA);
        var entryB = GetRegionById(regionB);

        if (entryA == null || entryB == null)
            return;

        // Add connection A -> B
        if (!entryA.ConnectedRegionIds.Contains(regionB))
        {
            entryA.ConnectedRegionIds.Add(regionB);
            entryA.ConnectionDetails.Add(new RegionConnectionInfo
            {
                TargetRegionId = regionB,
                TravelTimeMinutes = travelTime,
                DangerLevel = dangerLevel
            });
        }

        // Add connection B -> A (bidirectional)
        if (!entryB.ConnectedRegionIds.Contains(regionA))
        {
            entryB.ConnectedRegionIds.Add(regionA);
            entryB.ConnectionDetails.Add(new RegionConnectionInfo
            {
                TargetRegionId = regionA,
                TravelTimeMinutes = travelTime,
                DangerLevel = dangerLevel
            });
        }
    }

    /// <summary>
    /// Marks a region as discovered by the player.
    /// </summary>
    public void DiscoverRegion(Guid regionId)
    {
        var entry = GetRegionById(regionId);
        if (entry != null)
        {
            entry.IsDiscovered = true;
            entry.DiscoveredAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Sets the current region, discovering it if not already discovered.
    /// </summary>
    public void SetCurrentRegion(Guid regionId)
    {
        CurrentRegionId = regionId;
        DiscoverRegion(regionId);
    }

    /// <summary>
    /// Checks if travel is possible between two regions.
    /// </summary>
    public bool CanTravelTo(Guid fromRegionId, Guid toRegionId)
    {
        var fromRegion = GetRegionById(fromRegionId);
        if (fromRegion == null)
            return false;

        return fromRegion.ConnectedRegionIds.Contains(toRegionId);
    }

    /// <summary>
    /// Gets travel info between two connected regions.
    /// </summary>
    public RegionConnectionInfo? GetConnectionInfo(Guid fromRegionId, Guid toRegionId)
    {
        var fromRegion = GetRegionById(fromRegionId);
        if (fromRegion == null)
            return null;

        return fromRegion.ConnectionDetails.FirstOrDefault(c => c.TargetRegionId == toRegionId);
    }

    /// <summary>
    /// Creates a new world with a starting region.
    /// </summary>
    public static RegionMap CreateNew(string worldName, RegionMapEntry startingRegion)
    {
        var map = new RegionMap
        {
            WorldName = worldName,
            WorldId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        startingRegion.IsDiscovered = true;
        startingRegion.DiscoveredAt = DateTime.UtcNow;
        map.AddRegion(startingRegion);
        map.CurrentRegionId = startingRegion.RegionId;

        return map;
    }
}

/// <summary>
/// Entry for a single region in the world map.
/// Contains metadata and positioning for strategic map display.
/// </summary>
public class RegionMapEntry
{
    /// <summary>
    /// Unique identifier matching the .region file.
    /// </summary>
    public Guid RegionId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display name of this region.
    /// </summary>
    public string Name { get; set; } = "Unknown Region";

    /// <summary>
    /// Position on the strategic world map (in abstract coordinates).
    /// </summary>
    public float MapX { get; set; }
    public float MapY { get; set; }

    /// <summary>
    /// Relative path to the .region file.
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// Whether the player has discovered this region.
    /// </summary>
    public bool IsDiscovered { get; set; }

    /// <summary>
    /// When this region was discovered.
    /// </summary>
    public DateTime? DiscoveredAt { get; set; }

    /// <summary>
    /// Primary biome/theme of this region.
    /// </summary>
    public RegionBiome PrimaryBiome { get; set; } = RegionBiome.Temperate;

    /// <summary>
    /// Difficulty rating for this region (0-5).
    /// </summary>
    public int DifficultyRating { get; set; } = 1;

    /// <summary>
    /// Brief description shown in region info panel.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// IDs of directly connected regions.
    /// </summary>
    public List<Guid> ConnectedRegionIds { get; set; } = new();

    /// <summary>
    /// Detailed connection info for each connected region.
    /// </summary>
    public List<RegionConnectionInfo> ConnectionDetails { get; set; } = new();

    /// <summary>
    /// The generation seed used for this region.
    /// </summary>
    public int Seed { get; set; }

    /// <summary>
    /// Width of this region in cells.
    /// </summary>
    public int Width { get; set; } = RegionConfig.DefaultRegionWidth;

    /// <summary>
    /// Height of this region in cells.
    /// </summary>
    public int Height { get; set; } = RegionConfig.DefaultRegionHeight;

    /// <summary>
    /// Creates a RegionMapEntry from existing RegionData.
    /// </summary>
    public static RegionMapEntry FromRegionData(RegionData data, float mapX = 0f, float mapY = 0f)
    {
        return new RegionMapEntry
        {
            RegionId = data.RegionId,
            Name = data.Name,
            Seed = data.Seed,
            Width = data.Width,
            Height = data.Height,
            MapX = mapX,
            MapY = mapY,
            FilePath = $"{data.Name.ToLowerInvariant().Replace(" ", "_")}{RegionConfig.FileExtension}"
        };
    }
}

/// <summary>
/// Connection details between two regions.
/// </summary>
public class RegionConnectionInfo
{
    /// <summary>
    /// Target region for this connection.
    /// </summary>
    public Guid TargetRegionId { get; set; }

    /// <summary>
    /// Travel time in game minutes.
    /// </summary>
    public float TravelTimeMinutes { get; set; } = 60f;

    /// <summary>
    /// Danger level of this route (0.0-1.0).
    /// </summary>
    public float DangerLevel { get; set; }

    /// <summary>
    /// Whether this route is currently available (e.g., not blocked by weather).
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Optional description of the route.
    /// </summary>
    public string RouteDescription { get; set; } = "";
}

/// <summary>
/// Primary biome/theme of a region.
/// </summary>
public enum RegionBiome
{
    /// <summary>
    /// Moderate climate with mixed terrain.
    /// </summary>
    Temperate,

    /// <summary>
    /// Hot, humid climate with jungle terrain.
    /// </summary>
    Tropical,

    /// <summary>
    /// Cold climate with snow and ice.
    /// </summary>
    Arctic,

    /// <summary>
    /// Hot, dry climate with sandy terrain.
    /// </summary>
    Desert,

    /// <summary>
    /// Volcanic activity with lava and ash.
    /// </summary>
    Volcanic,

    /// <summary>
    /// Coastal/island with lots of beaches.
    /// </summary>
    Coastal,

    /// <summary>
    /// Swampy, marshy terrain.
    /// </summary>
    Swamp
}
