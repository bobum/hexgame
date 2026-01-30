namespace HexGame.Region;

/// <summary>
/// Configuration constants for the region streaming system.
/// Follows the same pattern as RenderingConfig and GenerationConfig.
/// </summary>
public static class RegionConfig
{
    #region Region Size

    /// <summary>
    /// Default region width in cells.
    /// </summary>
    public const int DefaultRegionWidth = 200;

    /// <summary>
    /// Default region height in cells.
    /// </summary>
    public const int DefaultRegionHeight = 200;

    /// <summary>
    /// Minimum region size (for small regions).
    /// </summary>
    public const int MinRegionSize = 50;

    /// <summary>
    /// Maximum region size (memory constraint).
    /// </summary>
    public const int MaxRegionSize = 300;

    #endregion

    #region File Format

    /// <summary>
    /// Magic number for .region files (ASCII: "HXRG" for HexRegion).
    /// </summary>
    public const uint FileMagicNumber = 0x47524858; // "HXRG" in little-endian

    /// <summary>
    /// Current file format version.
    /// Increment when making breaking changes to the format.
    /// </summary>
    public const uint FileVersion = 1;

    /// <summary>
    /// File extension for region files.
    /// </summary>
    public const string FileExtension = ".region";

    /// <summary>
    /// Directory for saved regions (relative to user://).
    /// </summary>
    public const string RegionsDirectory = "regions";

    /// <summary>
    /// Header size in bytes (magic + version + guid + width + height).
    /// </summary>
    public const int HeaderSize = 4 + 4 + 16 + 4 + 4; // 32 bytes

    #endregion

    #region Travel

    /// <summary>
    /// Minimum travel time in seconds (for loading buffer).
    /// Ensures loading completes before travel animation ends.
    /// </summary>
    public const float MinTravelTimeSeconds = 3.0f;

    /// <summary>
    /// Travel time per world-map distance unit (seconds).
    /// </summary>
    public const float TravelTimePerUnit = 0.5f;

    #endregion

    #region Memory

    /// <summary>
    /// Estimated memory per cell in bytes (HexCell + chunk contribution).
    /// Used for budget calculations.
    /// </summary>
    public const int EstimatedBytesPerCell = 500;

    /// <summary>
    /// Target memory budget for loaded region in MB.
    /// </summary>
    public const int TargetMemoryBudgetMB = 300;

    /// <summary>
    /// Packed cell data size in bytes for serialization.
    /// </summary>
    public const int PackedCellDataSize = 16;

    #endregion
}
