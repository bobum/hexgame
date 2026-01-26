using Godot;

/// <summary>
/// Debug configuration for hex map logging.
/// Set flags to true to enable verbose logging for specific systems.
/// All flags default to false for production performance.
/// </summary>
public static class HexDebug
{
    /// <summary>
    /// Enable logging for mesh triangulation operations.
    /// WARNING: Very verbose - logs every cell, direction, and triangle.
    /// </summary>
    public static bool LogTriangulation { get; set; } = false;

    /// <summary>
    /// Enable logging for road triangulation specifically.
    /// </summary>
    public static bool LogRoads { get; set; } = false;

    /// <summary>
    /// Enable logging for feature placement (urban, farm, plant, special).
    /// </summary>
    public static bool LogFeatures { get; set; } = false;

    /// <summary>
    /// Enable logging for material and texture loading.
    /// </summary>
    public static bool LogMaterials { get; set; } = false;

    /// <summary>
    /// Enable logging for pathfinding operations.
    /// </summary>
    public static bool LogPathfinding { get; set; } = false;

    /// <summary>
    /// Enable all debug logging. Use sparingly - significant performance impact.
    /// </summary>
    public static void EnableAll()
    {
        LogTriangulation = true;
        LogRoads = true;
        LogFeatures = true;
        LogMaterials = true;
        LogPathfinding = true;
        GD.Print("[HexDebug] All debug logging enabled");
    }

    /// <summary>
    /// Disable all debug logging for production performance.
    /// </summary>
    public static void DisableAll()
    {
        LogTriangulation = false;
        LogRoads = false;
        LogFeatures = false;
        LogMaterials = false;
        LogPathfinding = false;
    }

    /// <summary>
    /// Conditionally print a triangulation debug message.
    /// </summary>
    public static void PrintTriangulation(string message)
    {
        if (LogTriangulation) GD.Print(message);
    }

    /// <summary>
    /// Conditionally print a road debug message.
    /// </summary>
    public static void PrintRoad(string message)
    {
        if (LogRoads) GD.Print(message);
    }

    /// <summary>
    /// Conditionally print a feature debug message.
    /// </summary>
    public static void PrintFeature(string message)
    {
        if (LogFeatures) GD.Print(message);
    }

    /// <summary>
    /// Conditionally print a material loading debug message.
    /// </summary>
    public static void PrintMaterial(string message)
    {
        if (LogMaterials) GD.Print(message);
    }

    /// <summary>
    /// Conditionally print a pathfinding debug message.
    /// </summary>
    public static void PrintPathfinding(string message)
    {
        if (LogPathfinding) GD.Print(message);
    }
}
