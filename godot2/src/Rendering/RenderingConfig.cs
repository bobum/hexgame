namespace HexGame.Rendering;

/// <summary>
/// Configuration constants for the rendering system.
/// Controls chunking, LOD, culling, and fog parameters.
/// </summary>
public static class RenderingConfig
{
    #region Chunking

    /// <summary>
    /// Size of each chunk in world units.
    /// Chunks are square regions that group cells for efficient rendering.
    /// </summary>
    public const float ChunkSize = 16.0f;

    #endregion

    #region Level of Detail (LOD)

    /// <summary>
    /// Distance threshold for switching from HIGH to MEDIUM detail.
    /// Below this distance, full terrain detail with terraces is shown.
    /// </summary>
    public const float LodHighToMedium = 300.0f;

    /// <summary>
    /// Distance threshold for switching from MEDIUM to LOW detail.
    /// Below this distance, flat hexagons are shown.
    /// </summary>
    public const float LodMediumToLow = 500.0f;

    /// <summary>
    /// Maximum distance at which chunks are rendered.
    /// Chunks beyond this distance are culled entirely.
    /// </summary>
    public const float MaxRenderDistance = 800.0f;

    #endregion

    #region Fog

    /// <summary>
    /// Default distance at which fog begins.
    /// Keep tight like reference (12-30 range) for atmospheric effect.
    /// </summary>
    public const float DefaultFogNear = 15.0f;

    /// <summary>
    /// Default distance at which fog is fully opaque.
    /// </summary>
    public const float DefaultFogFar = 50.0f;

    /// <summary>
    /// Default fog density (0-1).
    /// </summary>
    public const float DefaultFogDensity = 0.5f;

    /// <summary>
    /// Minimum fog near distance for UI slider.
    /// </summary>
    public const float FogNearMin = 5.0f;

    /// <summary>
    /// Maximum fog near distance for UI slider.
    /// </summary>
    public const float FogNearMax = 500.0f;

    /// <summary>
    /// Minimum fog far distance for UI slider.
    /// </summary>
    public const float FogFarMin = 20.0f;

    /// <summary>
    /// Maximum fog far distance for UI slider.
    /// </summary>
    public const float FogFarMax = 1000.0f;

    #endregion

    #region Performance Monitoring

    /// <summary>
    /// Number of frames to track for performance history.
    /// </summary>
    public const int FrameHistorySize = 200;

    /// <summary>
    /// Target frame time in milliseconds (60 FPS).
    /// </summary>
    public const float TargetFrameTimeMs = 16.67f;

    /// <summary>
    /// Warning frame time threshold in milliseconds (30 FPS).
    /// </summary>
    public const float WarningFrameTimeMs = 33.33f;

    /// <summary>
    /// Width of the performance graph in pixels.
    /// </summary>
    public const int PerformanceGraphWidth = 200;

    /// <summary>
    /// Height of the performance graph in pixels.
    /// </summary>
    public const int PerformanceGraphHeight = 60;

    #endregion

    #region Control Panel

    /// <summary>
    /// Width of the control panel in pixels.
    /// </summary>
    public const int ControlPanelWidth = 260;

    /// <summary>
    /// Margin from screen edges in pixels.
    /// </summary>
    public const int PanelMargin = 10;

    #endregion

    #region Map Generation Defaults

    /// <summary>
    /// Default map width in cells.
    /// </summary>
    public const int DefaultMapWidth = 32;

    /// <summary>
    /// Default map height in cells.
    /// </summary>
    public const int DefaultMapHeight = 32;

    /// <summary>
    /// Minimum map width.
    /// </summary>
    public const int MinMapWidth = 10;

    /// <summary>
    /// Maximum map width.
    /// </summary>
    public const int MaxMapWidth = 80;

    /// <summary>
    /// Minimum map height.
    /// </summary>
    public const int MinMapHeight = 10;

    /// <summary>
    /// Maximum map height.
    /// </summary>
    public const int MaxMapHeight = 60;

    #endregion
}
