using System;
using System.Text.Json;
using HexGame.Camera;
using HexGame.Core;
using GodotFileAccess = Godot.FileAccess;

namespace HexGame.Debug;

/// <summary>
/// Debug state for reproducible screenshots.
/// Contains everything needed to reproduce a specific view.
/// </summary>
public record DebugState
{
    public int Seed { get; init; }
    public int MapWidth { get; init; }
    public int MapHeight { get; init; }
    public float TargetX { get; init; }
    public float TargetY { get; init; }
    public float TargetZ { get; init; }
    public float Distance { get; init; }
    public float Pitch { get; init; }
    public float Yaw { get; init; }
    public string? Description { get; init; }
    public DateTime CapturedAt { get; init; }

    /// <summary>
    /// Gets the camera state from this debug state.
    /// </summary>
    public CameraState GetCameraState() => new(
        new Vector3(TargetX, TargetY, TargetZ),
        Distance,
        Pitch,
        Yaw
    );
}

/// <summary>
/// Screenshot capture and debug state management for AI-assisted visual debugging.
///
/// Key bindings:
/// - F12: Take screenshot with current camera position
/// - F11: Position camera at debug view and take screenshot
/// - F10: Save current debug state (seed + camera) to file
/// - F9: Load and restore debug state from file
/// </summary>
public partial class ScreenshotCapture : Node
{
    #region Constants

    /// <summary>Screenshot output path.</summary>
    public const string ScreenshotPath = "C:/projects/hexgame/godot/debug_screenshot.png";

    /// <summary>Debug state file path.</summary>
    public const string DebugStatePath = "C:/projects/hexgame/godot/debug_state.json";

    /// <summary>Fixed camera settings for consistent debug screenshots.</summary>
    public const float DebugCameraDistance = 30.0f;
    public const float DebugCameraPitch = 45.0f;
    public const float DebugCameraYaw = 30.0f;

    #endregion

    #region Signals

    [Signal]
    public delegate void ScreenshotTakenEventHandler(string path);

    [Signal]
    public delegate void DebugStateSavedEventHandler(string path);

    [Signal]
    public delegate void DebugStateLoadedEventHandler(string path);

    #endregion

    #region State

    private MapCamera? _camera;
    private Node3D? _main;
    private CameraState? _pendingCameraState;
    private int _cameraRestoreDelayFrames;
    private bool _autoLoadRequested;
    private bool _autoScreenshotRequested;
    private int _frameDelayCounter;

    #endregion

    public override void _Ready()
    {
        GD.Print("[ScreenshotCapture] Ready - F12=screenshot, F11=debug screenshot, F10=save state, F9=load state");

        // Check for command line arguments for autonomous operation
        var args = OS.GetCmdlineArgs();
        var userArgs = OS.GetCmdlineUserArgs();

        GD.Print($"[ScreenshotCapture] Command line args: [{string.Join(", ", args)}]");
        GD.Print($"[ScreenshotCapture] User args: [{string.Join(", ", userArgs)}]");

        // Check both regular and user args
        var allArgs = new System.Collections.Generic.List<string>(args);
        allArgs.AddRange(userArgs);

        foreach (var arg in allArgs)
        {
            if (arg == "--load-debug-state" || arg == "--debug-state")
            {
                GD.Print("[ScreenshotCapture] Auto-load debug state requested via command line");
                _autoLoadRequested = true;
            }
            if (arg == "--auto-screenshot" || arg == "--screenshot")
            {
                GD.Print("[ScreenshotCapture] Auto-screenshot requested via command line");
                _autoScreenshotRequested = true;
            }
        }
    }

    /// <summary>
    /// Setup with camera and main node references.
    /// </summary>
    public void Setup(MapCamera camera, Node3D main)
    {
        _camera = camera;
        _main = main;
    }

    public override void _Process(double delta)
    {
        // Handle auto-load on startup (after a few frames to let everything initialize)
        if (_autoLoadRequested && _camera != null && _main != null)
        {
            _frameDelayCounter++;
            if (_frameDelayCounter >= 10) // Wait 10 frames for initialization
            {
                _autoLoadRequested = false;
                GD.Print("[ScreenshotCapture] Executing auto-load of debug state...");
                LoadDebugState();
            }
        }

        // Apply pending camera state after map regeneration (with delay for async)
        if (_pendingCameraState.HasValue && _camera != null)
        {
            if (_cameraRestoreDelayFrames > 0)
            {
                _cameraRestoreDelayFrames--;
                return; // Wait for delay
            }

            _camera.SetState(_pendingCameraState.Value);
            var state = _pendingCameraState.Value;
            GD.Print($"[ScreenshotCapture] Camera state restored: target=({state.Target.X:F1}, {state.Target.Y:F1}, {state.Target.Z:F1}), dist={state.Distance:F1}, pitch={state.Pitch:F1}, yaw={state.Yaw:F1}");
            _pendingCameraState = null;

            // If auto-screenshot was requested, take it after camera is positioned
            if (_autoScreenshotRequested)
            {
                _autoScreenshotRequested = false;
                // Wait a few more frames for the scene to render, then screenshot and quit
                CallDeferred(MethodName.AutoScreenshotAndQuit);
            }
        }
    }

    private async void AutoScreenshotAndQuit()
    {
        // Wait for scene to fully render
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        GD.Print("[ScreenshotCapture] Taking auto-screenshot...");
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

        var viewport = GetViewport();
        var image = viewport.GetTexture().GetImage();
        var error = image.SavePng(ScreenshotPath);

        if (error == Error.Ok)
        {
            GD.Print($"[ScreenshotCapture] Auto-screenshot saved to: {ScreenshotPath}");
        }
        else
        {
            GD.PushError($"[ScreenshotCapture] Failed to save auto-screenshot: {error}");
        }

        // Quit after screenshot
        GD.Print("[ScreenshotCapture] Auto-screenshot complete, quitting...");
        GetTree().Quit();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            switch (keyEvent.Keycode)
            {
                case Key.F12:
                    GD.Print("[ScreenshotCapture] F12 - Capturing screenshot...");
                    CaptureScreenshot();
                    break;

                case Key.F11:
                    GD.Print("[ScreenshotCapture] F11 - Debug camera + screenshot...");
                    CaptureDebugScreenshot();
                    break;

                case Key.F10:
                    GD.Print("[ScreenshotCapture] F10 - Saving debug state...");
                    SaveDebugState();
                    break;

                case Key.F9:
                    GD.Print("[ScreenshotCapture] F9 - Loading debug state...");
                    LoadDebugState();
                    break;
            }
        }
    }

    #region Screenshot Capture

    /// <summary>
    /// Takes a screenshot and saves to the fixed path.
    /// </summary>
    public async void CaptureScreenshot()
    {
        // Wait for the frame to render
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

        var viewport = GetViewport();
        var image = viewport.GetTexture().GetImage();

        var error = image.SavePng(ScreenshotPath);
        if (error == Error.Ok)
        {
            GD.Print($"[ScreenshotCapture] Screenshot saved to: {ScreenshotPath}");
            EmitSignal(SignalName.ScreenshotTaken, ScreenshotPath);
        }
        else
        {
            GD.PushError($"[ScreenshotCapture] Failed to save screenshot: {error}");
        }
    }

    /// <summary>
    /// Positions camera at debug view and takes screenshot.
    /// </summary>
    public async void CaptureDebugScreenshot()
    {
        if (_camera == null)
        {
            GD.PushError("[ScreenshotCapture] No camera set up");
            return;
        }

        // Calculate map center if we have access to main
        var center = Vector3.Zero;
        if (_main != null)
        {
            var mapWidth = (int)_main.Get("MapWidth");
            var mapHeight = (int)_main.Get("MapHeight");
            var centerQ = mapWidth / 2;
            var centerR = mapHeight / 2;
            var centerCoords = new HexCoordinates(centerQ, centerR);
            center = centerCoords.ToWorldPosition(0);
        }

        // Position camera
        PositionCameraForDebug(center);

        // Wait for camera to update
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // Capture
        CaptureScreenshot();
    }

    /// <summary>
    /// Positions camera at fixed debug position for consistent screenshots.
    /// </summary>
    public void PositionCameraForDebug(Vector3 centerTarget)
    {
        if (_camera == null)
        {
            GD.PushError("[ScreenshotCapture] No camera set up");
            return;
        }

        _camera.SetState(new CameraState(
            centerTarget,
            DebugCameraDistance,
            DebugCameraPitch,
            DebugCameraYaw
        ));

        GD.Print($"[ScreenshotCapture] Camera positioned at debug view (dist={DebugCameraDistance}, pitch={DebugCameraPitch}, yaw={DebugCameraYaw})");
    }

    #endregion

    #region Debug State Save/Load

    /// <summary>
    /// Saves the current debug state (seed + camera) to a JSON file.
    /// </summary>
    public void SaveDebugState(string? description = null)
    {
        if (_camera == null || _main == null)
        {
            GD.PushError("[ScreenshotCapture] Camera or main node not set up");
            return;
        }

        var cameraState = _camera.GetState();
        var seed = (int)_main.Get("CurrentSeed");
        var mapWidth = (int)_main.Get("MapWidth");
        var mapHeight = (int)_main.Get("MapHeight");

        var debugState = new DebugState
        {
            Seed = seed,
            MapWidth = mapWidth,
            MapHeight = mapHeight,
            TargetX = cameraState.Target.X,
            TargetY = cameraState.Target.Y,
            TargetZ = cameraState.Target.Z,
            Distance = cameraState.Distance,
            Pitch = cameraState.Pitch,
            Yaw = cameraState.Yaw,
            Description = description,
            CapturedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(debugState, new JsonSerializerOptions { WriteIndented = true });

        using var file = GodotFileAccess.Open(DebugStatePath, GodotFileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(json);
            GD.Print($"[ScreenshotCapture] Debug state saved to: {DebugStatePath}");
            GD.Print($"  Seed: {seed}, Map: {mapWidth}x{mapHeight}");
            GD.Print($"  Camera: target=({cameraState.Target.X:F1}, {cameraState.Target.Y:F1}, {cameraState.Target.Z:F1}), dist={cameraState.Distance:F1}, pitch={cameraState.Pitch:F1}, yaw={cameraState.Yaw:F1}");
            EmitSignal(SignalName.DebugStateSaved, DebugStatePath);
        }
        else
        {
            GD.PushError($"[ScreenshotCapture] Failed to open file for writing: {DebugStatePath}");
        }
    }

    /// <summary>
    /// Loads debug state from JSON file and restores it.
    /// </summary>
    public void LoadDebugState()
    {
        if (_camera == null || _main == null)
        {
            GD.PushError("[ScreenshotCapture] Camera or main node not set up");
            return;
        }

        if (!GodotFileAccess.FileExists(DebugStatePath))
        {
            GD.PushWarning($"[ScreenshotCapture] No debug state file found at: {DebugStatePath}");
            return;
        }

        using var file = GodotFileAccess.Open(DebugStatePath, GodotFileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError($"[ScreenshotCapture] Failed to open file: {DebugStatePath}");
            return;
        }

        var json = file.GetAsText();
        var debugState = JsonSerializer.Deserialize<DebugState>(json);
        if (debugState == null)
        {
            GD.PushError("[ScreenshotCapture] Failed to parse debug state JSON");
            return;
        }

        GD.Print($"[ScreenshotCapture] Loading debug state from: {DebugStatePath}");
        GD.Print($"  Seed: {debugState.Seed}, Map: {debugState.MapWidth}x{debugState.MapHeight}");
        if (!string.IsNullOrEmpty(debugState.Description))
        {
            GD.Print($"  Description: {debugState.Description}");
        }

        // Tell Main to skip CenterCamera after regeneration
        _main.Set("SkipNextCenterCamera", true);

        // Regenerate map with saved seed
        _main.Call("RegenerateWithSettings", debugState.MapWidth, debugState.MapHeight, debugState.Seed);

        // Store camera state to apply after map regenerates
        // We'll apply it after a delay to let async generation complete
        _pendingCameraState = debugState.GetCameraState();
        _cameraRestoreDelayFrames = 30; // Wait for async generation to complete

        EmitSignal(SignalName.DebugStateLoaded, DebugStatePath);
    }

    #endregion

    #region Static Helpers

    /// <summary>
    /// Gets the expected screenshot path for external tools.
    /// </summary>
    public static string GetScreenshotPath() => ScreenshotPath;

    /// <summary>
    /// Gets the debug state file path.
    /// </summary>
    public static string GetDebugStatePath() => DebugStatePath;

    /// <summary>
    /// Checks if a screenshot exists.
    /// </summary>
    public static bool ScreenshotExists() => GodotFileAccess.FileExists(ScreenshotPath);

    /// <summary>
    /// Checks if a debug state file exists.
    /// </summary>
    public static bool DebugStateExists() => GodotFileAccess.FileExists(DebugStatePath);

    /// <summary>
    /// Loads debug state from file without restoring it (for reading values).
    /// </summary>
    public static DebugState? ReadDebugState()
    {
        if (!GodotFileAccess.FileExists(DebugStatePath))
            return null;

        using var file = GodotFileAccess.Open(DebugStatePath, GodotFileAccess.ModeFlags.Read);
        if (file == null)
            return null;

        var json = file.GetAsText();
        return JsonSerializer.Deserialize<DebugState>(json);
    }

    #endregion
}
