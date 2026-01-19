using Godot;
using System.Text.Json;

/// <summary>
/// Camera utility for saving positions and taking screenshots.
///
/// Runtime usage:
///   F10 - Save current camera position to snapshot.json
///   F12/End - Take screenshot manually
///
/// Command line usage:
///   --snapshot=path.json  - Load position from JSON, take screenshot, quit
/// </summary>
public partial class ScreenshotCamera : Camera3D
{
    [Export] public string SnapshotPath = "user://snapshot.json";
    [Export] public string ScreenshotPath = "user://screenshot.png";

    private bool _pendingScreenshot = false;
    private double _screenshotTimer = 0;

    public override void _Ready()
    {
        var args = OS.GetCmdlineArgs();
        foreach (var arg in args)
        {
            if (arg.StartsWith("--snapshot="))
            {
                var jsonPath = arg.Replace("--snapshot=", "");
                if (LoadSnapshot(jsonPath))
                {
                    _pendingScreenshot = true;
                    _screenshotTimer = 0.5; // Wait for scene to render
                }
                else
                {
                    GD.PrintErr($"Failed to load snapshot: {jsonPath}");
                    GetTree().Quit(1);
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        // Handle delayed screenshot
        if (_pendingScreenshot)
        {
            _screenshotTimer -= delta;
            if (_screenshotTimer <= 0)
            {
                TakeScreenshot();
                _pendingScreenshot = false;
                GetTree().Quit();
            }
            return;
        }

        // F10 - Save camera position
        if (Input.IsKeyPressed(Key.F10))
        {
            SaveSnapshot();
        }

        // F12/End - Take screenshot manually
        if (Input.IsActionJustPressed("ui_end"))
        {
            TakeScreenshot();
        }
    }

    public void SaveSnapshot()
    {
        var snapshot = new CameraSnapshot
        {
            PositionX = Position.X,
            PositionY = Position.Y,
            PositionZ = Position.Z,
            RotationX = RotationDegrees.X,
            RotationY = RotationDegrees.Y,
            RotationZ = RotationDegrees.Z,
            Seed = 0 // TODO: Get from map generator when implemented
        };

        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });

        var path = ResolvePath(SnapshotPath);
        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(json);
            GD.Print($"Snapshot saved to: {path}");
        }
        else
        {
            GD.PrintErr($"Failed to save snapshot: {Godot.FileAccess.GetOpenError()}");
        }
    }

    public bool LoadSnapshot(string path)
    {
        var resolvedPath = ResolvePath(path);
        if (!Godot.FileAccess.FileExists(resolvedPath))
        {
            GD.PrintErr($"Snapshot file not found: {resolvedPath}");
            return false;
        }

        using var file = Godot.FileAccess.Open(resolvedPath, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"Failed to open snapshot: {Godot.FileAccess.GetOpenError()}");
            return false;
        }

        var json = file.GetAsText();
        var snapshot = JsonSerializer.Deserialize<CameraSnapshot>(json);
        if (snapshot == null)
        {
            GD.PrintErr("Failed to parse snapshot JSON");
            return false;
        }

        Position = new Vector3(snapshot.PositionX, snapshot.PositionY, snapshot.PositionZ);
        RotationDegrees = new Vector3(snapshot.RotationX, snapshot.RotationY, snapshot.RotationZ);

        GD.Print($"Loaded snapshot: pos=({Position.X}, {Position.Y}, {Position.Z}) rot=({RotationDegrees.X}, {RotationDegrees.Y}, {RotationDegrees.Z})");

        // TODO: Apply seed to map generator when implemented

        return true;
    }

    public void TakeScreenshot()
    {
        var viewport = GetViewport();
        var img = viewport.GetTexture().GetImage();

        var path = ResolvePath(ScreenshotPath);
        var error = img.SavePng(path);
        if (error == Error.Ok)
        {
            GD.Print($"Screenshot saved to: {path}");
        }
        else
        {
            GD.PrintErr($"Failed to save screenshot: {error}");
        }
    }

    private string ResolvePath(string path)
    {
        if (path.StartsWith("user://"))
        {
            return OS.GetUserDataDir() + "/" + path.Replace("user://", "");
        }
        return path;
    }
}

public class CameraSnapshot
{
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
    public float RotationX { get; set; }
    public float RotationY { get; set; }
    public float RotationZ { get; set; }
    public int Seed { get; set; }
}
