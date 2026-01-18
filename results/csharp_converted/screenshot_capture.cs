using Godot;
using Godot.Collections;


//# Automatic screenshot capture for AI-assisted visual debugging

//# Saves screenshots to a fixed location for external analysis
// Screenshot output path - save to project root for easy access
[GlobalClass]
public partial class ScreenshotCapture : Godot.Node
{
	public const string SCREENSHOT_PATH = "C:/projects/hexgame/godot/debug_screenshot.png";


	// Fixed camera settings for consistent screenshots - match target images
	public const double DEBUG_CAMERA_DISTANCE = 30.0;
	public const double DEBUG_CAMERA_PITCH = 45.0;
	public const double DEBUG_CAMERA_YAW = 30.0;


	// Auto-capture settings
	public const double AUTO_CAPTURE_DELAY =  - 1.0;
	// Set to -1 to disable auto-capture
	public const bool AUTO_QUIT_AFTER_CAPTURE = false;
	public const int DEBUG_SEED = 12345;

	// Fixed seed for consistent screenshots
	// Reference to map camera
	public Godot.MapCamera Camera = null;
	public double AutoCaptureTimer =  - 1.0;
	public bool HasAutoCaptured = false;


	// Signals
	[Signal]
	public delegate void ScreenshotTakenEventHandler(String path);
	[Signal]
	public delegate void CameraPositionedEventHandler();


	public override void _Ready()
	{
		GD.Print("[ScreenshotCapture] Ready - Auto-capture in %.1f seconds" % AUTO_CAPTURE_DELAY);
		AutoCaptureTimer = AUTO_CAPTURE_DELAY;
	}


	public override void _Process(double delta)
	{
		if(AutoCaptureTimer > 0)
		{
			AutoCaptureTimer -= delta;
			if(AutoCaptureTimer <= 0 && !HasAutoCaptured)
			{
				HasAutoCaptured = true;
				_DoAutoCapture();
			}
		}
	}


	protected void _DoAutoCapture()
	{
		GD.Print("[ScreenshotCapture] Starting auto-capture sequence...");
		var main = GetParent();


		// Use fixed seed for consistent screenshots
		if(main && main.HasMethod("regenerate_with_settings"))
		{
			GD.Print("[ScreenshotCapture] Using fixed seed: %d" % DEBUG_SEED);
			main.RegenerateWithSettings(main.MapWidth, main.MapHeight, DEBUG_SEED);

			// Wait for regeneration to complete
			await ToSignal(GetTree().CreateTimer(1.0), "Timeout");
		}


		// Calculate map center
		var center = Vector3.Zero;
		if(main && main && main.Grid.Contains("grid"))
		{
			var center_q = main.MapWidth / 2;
			var center_r = main.MapHeight / 2;
			var center_coords = HexCoordinates.New(center_q, center_r);
			center = center_coords.ToWorldPosition(0);
		}


		// Position camera and capture
		var path = await;CaptureDebugScreenshot(center);

		if(path != "" && AUTO_QUIT_AFTER_CAPTURE)
		{
			GD.Print("[ScreenshotCapture] Screenshot complete, quitting...");
			await ToSignal(GetTree().CreateTimer(0.5), "Timeout");
			GetTree().Quit();
		}
	}


	//# Setup with camera reference
	public void Setup(Godot.MapCamera map_camera)
	{
		Camera = map_camera;
	}


	//# Take a screenshot and save to the fixed path
	public String CaptureScreenshot()
	{

		// Wait for the frame to render
		await ToSignal(Godot.RenderingServer, "FramePostDraw");


		// Get the viewport image
		var viewport = GetViewport();
		var image = viewport.GetTexture().GetImage();


		// Save to project directory
		var error = image.SavePng(SCREENSHOT_PATH);
		if(error == OK)
		{
			GD.Print("[ScreenshotCapture] Screenshot saved to: %s" % SCREENSHOT_PATH);
			EmitSignal("ScreenshotTaken", SCREENSHOT_PATH);
			return SCREENSHOT_PATH;
		}
		else
		{
			GD.PushError("[ScreenshotCapture] Failed to save screenshot: %s" % error);
			return "";
		}
	}


	//# Position camera at a fixed debug position for consistent screenshots
	public void PositionCameraForDebug(Vector3 center_target = Vector3.Zero)
	{
		if(!Camera)
		{
			GD.PushError("[ScreenshotCapture] No camera set up");
			return ;
		}


		// Set fixed camera position
		Camera.SetTarget(center_target);
		Camera.TargetDistance = DEBUG_CAMERA_DISTANCE;
		Camera.TargetPitch = DEBUG_CAMERA_PITCH;
		Camera.TargetYaw = DEBUG_CAMERA_YAW;


		// Also set current values to snap immediately
		Camera.Distance = DEBUG_CAMERA_DISTANCE;
		Camera.Pitch = DEBUG_CAMERA_PITCH;
		Camera.Yaw = DEBUG_CAMERA_YAW;

		GD.Print("[ScreenshotCapture] Camera positioned at debug view (dist=%.1f, pitch=%.1f, yaw=%.1f)" % new Array{
					DEBUG_CAMERA_DISTANCE, DEBUG_CAMERA_PITCH, DEBUG_CAMERA_YAW, 
					});
		EmitSignal("CameraPositioned");
	}


	//# Position camera and take screenshot in one call
	public String CaptureDebugScreenshot(Vector3 center_target = Vector3.Zero)
	{
		PositionCameraForDebug(center_target);


		// Wait a couple frames for camera to update and scene to render
		await ToSignal(GetTree(), "ProcessFrame");
		await ToSignal(GetTree(), "ProcessFrame");

		return await;CaptureScreenshot();
	}


	//# Get the expected screenshot path for external tools
	public static String GetScreenshotPath()
	{
		return SCREENSHOT_PATH;
	}


	//# Check if a screenshot exists
	public static bool ScreenshotExists()
	{
		return FileAccess.FileExists(SCREENSHOT_PATH);
	}


	public override void _Input(Godot.InputEvent event)
	{
		if(event is Godot.InputEventKey)
		{ && event.Pressed;;;//PANIC! <:> unexpected at Token(type=':', value=':', lineno=140, index=4394, end=4395)

			{
				// F12 - Take screenshot with current camera position
				if(event.Keycode == KEY_F12)
				{
					GD.Print("[ScreenshotCapture] Capturing screenshot...");
					CaptureScreenshot();
				}

				// F11 - Position camera at debug view and take screenshot
				else if(event.Keycode == KEY_F11)
				{
					GD.Print("[ScreenshotCapture] Setting debug camera and capturing...");

					// Calculate map center if we have access to it
					var main = GetParent();
					var center = Vector3.Zero;
					if(main && main && main.Grid.Contains("grid"))
					{
						var center_q = main.MapWidth / 2;
						var center_r = main.MapHeight / 2;
						var center_coords = HexCoordinates.New(center_q, center_r);
						center = center_coords.ToWorldPosition(0);
					}
					CaptureDebugScreenshot(center);
				}
			}
		}


	}
}